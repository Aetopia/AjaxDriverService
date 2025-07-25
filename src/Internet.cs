using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

static class Internet
{
    static readonly HttpClient _client = new();

    internal static async Task<Stream> GetStreamAsync(string address)
    {
        return await _client.GetStreamAsync(address);
    }

    internal static async Task<HttpResponseMessage> GetAsync(Uri address)
    {
        return await _client.GetAsync(address, HttpCompletionOption.ResponseHeadersRead);
    }
}