using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using static System.Environment;

static class Downloader
{
    internal static async Task<string> GetAsync(Uri address, Action<int> action)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetFileName(address.PathAndQuery));

        using var message = await Internet.GetAsync(address);
        message.EnsureSuccessStatusCode();

        using Stream source = await message.Content.ReadAsStreamAsync(), destination = File.Create(path);

        var progress = 0; var buffer = new byte[SystemPageSize];
        double total = 0, length = message.Content.Headers.ContentLength ?? 0;

        double current; while ((current = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            if (length > 0)
            {
                var result = (int)((total += current) / length * 100);
                if (progress != result) action(progress = result);
            }
            await destination.WriteAsync(buffer, 0, buffer.Length);
        }

        return path;
    }
}