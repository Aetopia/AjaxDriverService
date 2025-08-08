using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Win32;
using static System.Environment;

public static class AjaxDriverService
{
    public sealed class Driver
    {
        public readonly string? Name;

        public readonly string? Url;

        public readonly bool IsCrd;

        internal Driver(string? name, string? url, bool isCrd)
        {
            Name = WebUtility.UrlDecode(name);
            Url = $"{new UriBuilder(url) { Host = "international.download.nvidia.com" }.Uri}";
            IsCrd = isCrd;
        }
    }

    const string Uri = "https://gfwsl.geforce.com/services_toolkit/services/com/nvidia/services/AjaxDriverService.php?func=DriverManualLookup&osCode={0}&is64bit={1}&deviceID={2}&dch={3}&upCRD={4}";

    static readonly string _uri; static readonly HttpClient _httpClient = new();

    static AjaxDriverService()
    {
        var osVersion = OSVersion.Version;
        var osCode = $"{osVersion.Major}.{osVersion.Minor}";
        var is64bit = Is64BitOperatingSystem ? 1 : 0;
        _uri = string.Format(Uri, osCode, is64bit, "{0}", "{1}", "{2}");
    }

    static async Task<(string Name, string Url)?> ResolveDriverAsync(string uri)
    {
        using var stream = await _httpClient.GetStreamAsync(uri);
        using var reader = JsonReaderWriterFactory.CreateJsonReader(stream, XmlDictionaryReaderQuotas.Max);

        var json = XElement.Load(reader);

        var name = json.Descendants("Name").FirstOrDefault()?.Value;
        if (name is null) return null;

        return new(name, json.Descendants("DownloadURL").First().Value);
    }

    static async Task GetDriverAsync(ConcurrentDictionary<string, ReadOnlyCollection<Driver>> devices, string deviceId)
    {
        List<Driver>? drivers = null;

        var dchTask = ResolveDriverAsync(string.Format(_uri, deviceId, 1, 0));
        var standardTask = ResolveDriverAsync(string.Format(_uri, deviceId, 0, 0));

        await Task.WhenAll([dchTask, standardTask]);
        (string Name, string Url)? dch = await dchTask, standard = await standardTask;

        if (dch is not null)
        {
            (drivers ??= []).Add(new(dch?.Name, dch?.Url, false));
            await ResolveCrdDriverAsync(drivers, deviceId, true);
        }
        else if (standard is not null)
        {
            (drivers ??= []).Add(new(standard?.Name, standard?.Url, false));
            await ResolveCrdDriverAsync(drivers, deviceId, false);
        }

        if (drivers?.Count > 0) devices.TryAdd(deviceId, new(drivers));
    }

    static async Task ResolveCrdDriverAsync(List<Driver> drivers, string deviceId, bool dch)
    {
        var crdUri = string.Format(_uri, deviceId, dch ? 1 : 0, 1);
        var crd = await ResolveDriverAsync(crdUri);
        if (crd is not null) drivers.Add(new(crd?.Name, crd?.Url, true));
    }

    public static ReadOnlyDictionary<string, ReadOnlyCollection<Driver>> DriverManualLookup()
    {
        List<Task> tasks = [];
        ConcurrentDictionary<string, ReadOnlyCollection<Driver>> devices = [];

        using var registryKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\PCI");
        foreach (var subKeyName in registryKey.GetSubKeyNames())
        {
            var substrings = subKeyName.Split('&');
            if (substrings[0] is not "VEN_10DE") continue;

            var deviceID = substrings[1].Split('_')[1];
            tasks.Add(GetDriverAsync(devices, deviceID));
        }

        Task.WhenAll(tasks).GetAwaiter().GetResult(); return new(devices);
    }
}