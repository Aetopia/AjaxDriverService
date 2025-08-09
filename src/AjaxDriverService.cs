using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Win32;
using static System.Environment;

static class AjaxDriverService
{
    const string Uri = "https://gfwsl.geforce.com/services_toolkit/services/com/nvidia/services/AjaxDriverService.php?func=DriverManualLookup&osCode={0}&is64bit={1}&deviceID={2}&dch={3}&upCRD={4}";

    static readonly string _uri;

    static readonly HttpClient _httpClient = new();

    static AjaxDriverService()
    {
        var osVersion = OSVersion.Version;
        var osCode = $"{osVersion.Major}.{osVersion.Minor}";
        var is64bit = Is64BitOperatingSystem ? 1 : 0;
        _uri = string.Format(Uri, osCode, is64bit, "{0}", "{1}", "{2}");
    }

    static async Task<Driver?> GetDriverFromUriAsync(string uri)
    {
        using var stream = await _httpClient.GetStreamAsync(uri);
        using var reader = JsonReaderWriterFactory.CreateJsonReader(stream, XmlDictionaryReaderQuotas.Max);

        var json = XElement.Load(reader);

        var name = json.Descendants("Name").FirstOrDefault()?.Value;
        if (name is null) return null;

        var version = json.Descendants("Version").First().Value;

        var url = json.Descendants("DownloadURL").First().Value;
        UriBuilder builder = new(url) { Host = "international.download.nvidia.com" };

        return new(WebUtility.UrlDecode(name), version, builder.Uri);
    }

    static async Task GetDriversForDeviceIdAsync(ConcurrentBag<Device> devices, string deviceId)
    {
        var dchUri = string.Format(_uri, deviceId, 1, 0);
        var standardUri = string.Format(_uri, deviceId, 0, 0);

        var dchTask = GetDriverFromUriAsync(dchUri);
        var standardTask = GetDriverFromUriAsync(standardUri);

        await Task.WhenAll(dchTask, standardTask);

        var arg = -1; List<Driver>? drivers = null;
        Driver? dch = await dchTask, standard = await standardTask;

        if (dch is not null)
        {
            arg = 1;
            (drivers ??= []).Add(dch);
        }
        else if (standard is not null)
        {
            arg = 0;
            (drivers ??= []).Add(standard);
        }

        if (drivers is null) return;

        if (arg is not -1)
        {
            var crdUri = string.Format(_uri, deviceId, arg, 1);
            var crd = await GetDriverFromUriAsync(crdUri);
            if (crd is not null) drivers.Add(crd);
        }

        devices.Add(new(deviceId, drivers));
    }

    internal static async Task<ConcurrentBag<Device>> DriverManualLookupAsync()
    {
        List<Task> tasks = []; ConcurrentBag<Device> devices = [];

        using var registryKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\PCI");
        foreach (var subKeyName in registryKey.GetSubKeyNames())
        {
            var substrings = subKeyName.Split('&');
            if (substrings[0] is not "VEN_10DE") continue;

            var deviceId = substrings[1].Split('_')[1];
            tasks.Add(GetDriversForDeviceIdAsync(devices, deviceId));
        }

        await Task.WhenAll(tasks); return devices;
    }
}