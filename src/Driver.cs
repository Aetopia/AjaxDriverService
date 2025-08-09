using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

sealed class Driver
{
    const string Script = """
@echo off
title {1} > nul 2>&1
 
chcp 65001 > nul 2>&1
cd "%~dp0" > nul 2>&1

rd /q /s "{0}" > nul 2>&1
md "{0}" > nul 2>&1

echo Downloading {1}...
"{2}" -L "{3}" -o "{4}"

echo Downloading 7-Zip...
"{2}" -L "https://www.7-zip.org/a/7zr.exe" -o "{5}"

echo Extracting {1}...
"{5}" x -bso0 -bsp1 -bse1 -aoa "{4}" "Display.Driver" "NVI2" "EULA.txt" "ListDevices.txt" "setup.cfg" "setup.exe" -o"{0}"

"{6}" "{0}"

del "{4}"
del "{5}"
del "%~f0"
""";

    static readonly string _curl, _cmd, _temp, _explorer;

    readonly string _string;

    readonly Uri _uri;

    static Driver()
    {
        string system = Environment.GetFolderPath(Environment.SpecialFolder.System);
        _temp = Path.GetTempPath();
        _explorer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
        _cmd = Path.Combine(system, "cmd.exe");
        _curl = Path.Combine(system, "curl.exe");
    }

    internal Driver(string name, string version, Uri uri)
    {
        _string = $"{name} ({version})";
        _uri = uri;
    }

    public override string ToString() => _string;

    internal async Task GetAsync()
    {
        var source = Path.Combine(_temp, Path.GetFileName(_uri.PathAndQuery));
        var destination = source.Substring(0, source.LastIndexOf('.'));
        var archiver = Path.Combine(destination, "7zr.exe");

        var path = Path.Combine(_temp, $"{Path.GetFileNameWithoutExtension(_uri.PathAndQuery)}.cmd");
        using StreamWriter writer = new(path) { AutoFlush = true };
        await writer.WriteAsync(string.Format(Script, destination, this, _curl, _uri, source, archiver, _explorer));

        using (Process.Start(new ProcessStartInfo { UseShellExecute = false, FileName = _cmd, Arguments = $"/c \"{path}\"" })) { }
    }
}