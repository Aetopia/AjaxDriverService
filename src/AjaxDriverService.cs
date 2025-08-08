using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Win32;
using static System.Environment;

public static class AjaxDriverService
{
    const string Uri = "https://gfwsl.geforce.com/services_toolkit/services/com/nvidia/services/AjaxDriverService.php?func=DriverManualLookup&osCode={0}&is64bit={1}&deviceID={2}&dch={3}&upCRD={4}";

    static readonly string _uri; static readonly HttpClient _httpClient = new();

    static AjaxDriverService()
    {
        var osVersion = OSVersion.Version;
        var osCode = $"{osVersion.Major}.{osVersion.Minor}";
        var is64bit = Is64BitOperatingSystem ? 1 : 0;
        _uri = string.Format(Uri, osCode, is64bit, "{0}", "{1}", "{2}");
    }

    static async Task<Tuple<string, Uri>?> ParseDriverAsync(string uri)
    {
        using var stream = await _httpClient.GetStreamAsync(uri);
        using var reader = JsonReaderWriterFactory.CreateJsonReader(stream, XmlDictionaryReaderQuotas.Max);

        var json = XElement.Load(reader);

        var name = json.Descendants("Name").FirstOrDefault()?.Value;
        if (name is null) return null;

        var url = json.Descendants("DownloadURL").First().Value;
        var builder = new UriBuilder(url) { Host = "international.download.nvidia.com" };

        return new(WebUtility.UrlDecode(name), builder.Uri);
    }

    static async Task GetDriverAsync(ConcurrentDictionary<string, List<Tuple<string, Uri>>> devices, string deviceId)
    {
        var standardUri = string.Format(_uri, deviceId, 0, 0);
        var dchUri = string.Format(_uri, deviceId, 1, 0);

        Task<Tuple<string, Uri>?> standardTask = ParseDriverAsync(standardUri), dchTask = ParseDriverAsync(dchUri);
        await Task.WhenAll(standardTask, dchTask);

        List<Tuple<string, Uri>> drivers = [];
        Tuple<string, Uri>? dch = await dchTask, standard = await standardTask;

        var flag = -1;
        if (dch is not null) { flag = 1; drivers.Add(dch); }
        else if (standard is not null) { flag = 0; drivers.Add(standard); }

        if (flag > -1)
        {
            var crdUri = string.Format(_uri, deviceId, flag, 1);
            var crd = await ParseDriverAsync(crdUri);
            if (crd is not null) drivers.Add(crd);
        }

        if (drivers.Count > 0) devices.TryAdd(deviceId, drivers);
    }

    public static async Task DriverManualLookupAsync()
    {
        List<Task> tasks = [];
        ConcurrentDictionary<string, List<Tuple<string, Uri>>> devices = [];

        using var registryKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\PCI");
        foreach (var subKeyName in registryKey.GetSubKeyNames())
        {
            var substrings = subKeyName.Split('&');
            if (substrings[0] is not "VEN_10DE") continue;

            var deviceId = substrings[1].Split('_')[1];
            tasks.Add(GetDriverAsync(devices, deviceId));
        }

        await Task.WhenAll(tasks);

        Console.WriteLine(new JavaScriptSerializer().Serialize(devices));
    }
}