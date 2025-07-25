using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Win32;
using static System.Environment;

static class Drivers
{
    const string Format = "https://gfwsl.geforce.com/services_toolkit/services/com/nvidia/services/AjaxDriverService.php?func=DriverManualLookup&osCode={0}&is64bit={1}&deviceID={2}&dch={3}&upCRD={4}";

    static readonly string _format = string.Format(Format, $"{OSVersion.Version.Major}.{OSVersion.Version.Minor}", Is64BitOperatingSystem ? 1 : 0, "{0}", "{1}", "{2}");

    static async Task<Dictionary<string, string>> GetDriversAsync(string value)
    {
        Dictionary<string, string> collection = [];

        var format = string.Format(_format, value, "{0}", "{1}");

        Task<Tuple<string, string>>[] tasks = [GetDriverAsync(string.Format(format, 0, 0)), GetDriverAsync(string.Format(format, 1, 0))];
        await Task.WhenAll(tasks);

        Tuple<string, string> standard = await tasks[0], dch = await tasks[1];

        if (dch is not null)
        {
            collection.Add(dch.Item1, dch.Item2);
            dch = await GetDriverAsync(string.Format(format, 1, 1));
            if (dch is not null) collection.Add(dch.Item1, dch.Item2);
        }
        else if (standard is not null)
        {
            collection.Add(standard.Item1, standard.Item2);
            standard = await GetDriverAsync(string.Format(format, 0, 1));
            if (standard is not null) collection.Add(standard.Item1, standard.Item2);
        }

        return collection;
    }

    static async Task<Tuple<string, string>> GetDriverAsync(string address)
    {
        using var stream = await Internet.GetStreamAsync(address);
        using var reader = JsonReaderWriterFactory.CreateJsonReader(stream, XmlDictionaryReaderQuotas.Max);

        var source = XElement.Load(reader);

        var key = source.Descendants("Name").FirstOrDefault();
        if (key is null) return null;

        var value = source.Descendants("DownloadURL").FirstOrDefault();
        if (value is null) return null;

        return new(WebUtility.UrlDecode(key.Value), $"{new UriBuilder(value.Value) { Host = "international.download.nvidia.com" }.Uri}");
    }

    internal static async Task<Dictionary<string, Dictionary<string, string>>> GetAsync()
    {
        Dictionary<string, Task<Dictionary<string, string>>> tasks = [];

        using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\PCI");

        foreach (var item in key.GetSubKeyNames())
        {
            var array = item.Split('&');
            if (array[0] is not "VEN_10DE") continue;

            var value = array[1].Split('_')[1];
            tasks.Add(value, GetDriversAsync(value));
        }

        await Task.WhenAll(tasks.Values);

        Dictionary<string, Dictionary<string, string>> collection = [];
        foreach (var task in tasks) if ((await task.Value).Count > 0) collection.Add(task.Key, await task.Value); return collection;
    }
}