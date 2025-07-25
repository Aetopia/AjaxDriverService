using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

static class Dearchiver
{
    const string Format = "x -bso0 -bsp1 -bse1 -aoa \"{0}\" Display.Driver NVI2 EULA.txt ListDevices.txt setup.cfg setup.exe -o\"{1}\"";

    static readonly string _path = Path.Combine(Path.GetTempPath(), $"{Path.GetRandomFileName()}.exe");

    static Dearchiver() => AppDomain.CurrentDomain.ProcessExit += (_, _) => { try { File.Delete(_path); } catch { } };

    static readonly SemaphoreSlim _semaphore = new(1, 1);

    static async Task DownloadAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (File.Exists(_path)) return;

            using var source = await Internet.GetStreamAsync("https://www.7-zip.org/a/7zr.exe");
            using var destination = File.Create(_path);

            await source.CopyToAsync(destination);
        }
        finally { _semaphore.Release(); }

        return;
    }

    static async Task WriteSetupAsync(string path)
    {
        using var stream = File.Open(Path.Combine(path, @"setup.cfg"), FileMode.Open);
        var source = XElement.Load(stream);

        foreach (var item in source.Element("manifest").Elements().ToArray())
        {
            var attribute = item.Attribute("name")?.Value;
            if (attribute is "${{EulaHtmlFile}}" or "${{FunctionalConsentFile}}" or "${{PrivacyPolicyFile}}")
                item.Remove();
        }

        using StreamWriter writer = new(stream, Encoding.ASCII); stream.SetLength(0);
        await writer.WriteAsync($"{source}");
    }

    static async Task WritePresentationsAsync(string path)
    {
        using var stream = File.Open(Path.Combine(path, @"NVI2\presentations.cfg"), FileMode.Open);
        var source = XElement.Load(stream);

        foreach (var item in source.Element("properties").Elements())
        {
            var attribute = item.Attribute("name")?.Value;
            if (attribute is "ProgressPresentationUrl" or "ProgressPresentationSelectedPackageUrl")
                item.Attribute("value").Value = string.Empty;
        }

        using StreamWriter writer = new(stream, Encoding.ASCII); stream.SetLength(0);
        await writer.WriteAsync($"{source}");
    }

    internal static async Task<string> GetAsync(string value, Action<int> action)
    {
        await DownloadAsync();

        var progress = 0;
        var path = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(value));

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = _path,
            Arguments = string.Format(Format, value, path),
            UseShellExecute = false,
            RedirectStandardOutput = true
        });

        try
        {
            while (!process.HasExited)
            {
                await Task.Yield();

                var @string = (await process.StandardOutput.ReadLineAsync()).Trim();
                if (string.IsNullOrWhiteSpace(@string)) continue;

                var strings = @string.Split('%');
                if (strings.Length > 0 || !int.TryParse(strings[0], out var result))
                    continue;

                if (progress != result)
                    action(progress = result);
            }
        }
        finally { try { process.Kill(); } catch { } }

        await Task.WhenAll(WritePresentationsAsync(path), WriteSetupAsync(path));

        return path;
    }
}