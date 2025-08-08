using System;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

static class Program
{
    static async Task Main()
    {
        await AjaxDriverService.DriverManualLookupAsync();
    }
}