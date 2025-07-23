using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Win32;
using static System.Environment;

static class Drivers
{
   const string Format = "https://gfwsl.geforce.com/services_toolkit/services/com/nvidia/services/AjaxDriverService.php?func=DriverManualLookup&osCode={0}&is64bit={1}&deviceID={2}&dch={3}&upCRD={4}";

   static readonly string Address = string.Format(Format, $"{OSVersion.Version.Major}.{OSVersion.Version.Minor}", Is64BitOperatingSystem ? 1 : 0, "{0}", "{1}", "{2}");

   static readonly HttpClient Client = new();

   static async Task<Dictionary<string, string>> DriversAsync(string value)
   {
      Dictionary<string, string> collection = [];

      var format = string.Format(Address, value, "{0}", "{1}");

      var standard = UriAsync(string.Format(format, 0, 0));
      var dch = UriAsync(string.Format(format, 1, 0));
      await Task.WhenAll([standard, dch]);

      if (await standard is not null)
         collection.Add("Standard", await standard);

      if (await dch is not null)
         collection.Add("DCH", await dch);

      if (collection.Any())
      {
         standard = UriAsync(string.Format(format, 0, 1));
         dch = UriAsync(string.Format(format, 1, 1));
         await Task.WhenAll([standard, dch]);

         if (await standard is not null)
            collection.Add("Standard Studio", await standard);

         if (await dch is not null)
            collection.Add("DCH Studio", await dch);
      }

      return collection;
   }

   static async Task<string> UriAsync(string value)
   {
      using var stream = await Client.GetStreamAsync(value);
      using var reader = JsonReaderWriterFactory.CreateJsonReader(stream, XmlDictionaryReaderQuotas.Max);

      foreach (var item in XElement.Load(reader).Descendants("DownloadURL"))
         return $"{new UriBuilder(item.Value) { Host = "international.download.nvidia.com" }.Uri}";
      return null;
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
         tasks.Add(value, DriversAsync(value));
      }

      await Task.WhenAll(tasks.Values);

      Dictionary<string, Dictionary<string, string>> collection = [];

      foreach (var task in tasks)
         if ((await task.Value).Any())
            collection.Add(task.Key, await task.Value);

      return collection;
   }
}