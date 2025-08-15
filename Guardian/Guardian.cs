using Microsoft.Win32;
using System.Diagnostics;
using FireflySR.Proxy.Common;

namespace FireflySR.Proxy.Guardian;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal class Program
{
    static async Task Main(string[] args)
    {
        Logger.Init();

        if (args.Length != 1 || !int.TryParse(args[0], out var watchPid))
        {
            Logger.Info("Usage: Guardian [watch-pid]");
            Environment.Exit(1);
            return;
        }
        
        Logger.Info("Guardian start...");
        Process proc;
        try
        {
            proc = Process.GetProcessById(watchPid);
            Logger.Info($"Guardian found process {proc.ProcessName} : {watchPid}");
        }
        catch
        {
            DisableSystemProxy();
            Environment.Exit(2);
            return;
        }

        while (!proc.HasExited)
        {
            await Task.Delay(1000);
        }
        DisableSystemProxy();
    }

    private static void DisableSystemProxy()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true);

            key?.SetValue("ProxyEnable", 0);
            Logger.Info($"Guardian successfully disabled System Proxy.");
        }
        catch (Exception ex)
        {
            Logger.Fail($"Failed to disable system proxy. Exception: {ex}");
        }
    }
}
