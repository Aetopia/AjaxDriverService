using System;
using System.Threading;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static void Main()
    {
        using Mutex mutex = new(false, "FEA883CD-1179-46E0-B917-7C879159EDAB", out var createdNew);
        if (!createdNew) return;

        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        Application.EnableVisualStyles();
        Application.Run(new MainForm());
    }
}