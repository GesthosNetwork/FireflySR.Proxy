using Microsoft.Win32;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FireflySR.Proxy.Common;

namespace FireflySR.Proxy
{
    internal class Program
    {
        private const string Title = "[ FIREFLY SR | PROXY ]";
        private const string ConfigPath = "config.json";
        private const string GuardianPath = "tool/Guardian.exe";

        private static ProxyService s_proxyService = null!;
        private static bool s_clearupd = false;

        static async Task Main(string[] args)
        {
            Logger.Init("INFO", Title);
			AppDomain.CurrentDomain.ProcessExit += (_, _) => Logger.Close();

            if (!File.Exists(ConfigPath))
            {
                Logger.Error("Missing config.json. Please create and configure it properly.\nPress ENTER to exit...");
                Console.ReadLine();
                Environment.Exit(1);
            }

            ProxyConfig? conf = null;
            try
            {
                conf = JsonSerializer.Deserialize<ProxyConfig>(File.ReadAllText(ConfigPath));
            }
            catch (Exception ex)
            {
                Logger.Hint($"Failed to parse config.json: {ex.Message}\nPress ENTER to exit...");
                Console.ReadLine();
                Environment.Exit(1);
            }

            if (conf == null)
            {
                Logger.Hint("Invalid config.json format. Please fix or regenerate.\nPress ENTER to exit...");
                Console.ReadLine();
                Environment.Exit(1);
            }

            CheckProxy();

            s_proxyService = new ProxyService(conf.DestinationHost, conf.DestinationPort, conf);

            _ = Task.Run(WatchGuardianAsync);

            AppDomain.CurrentDomain.ProcessExit += (_, _) => OnProcessExit();
            Console.CancelKeyPress += (_, _) => OnProcessExit();

            await Task.Delay(-1);
        }

        private static async Task WatchGuardianAsync()
        {
            var proc = StartGuardian();
            if (proc == null)
            {
                Logger.Fail("Guardian start failed. Your proxy settings may not recover after closing.");
                return;
            }

            while (!proc.HasExited)
                await Task.Delay(1000);

            Logger.Info("! Guardian exit");
            OnProcessExit();
            Console.ReadLine();
            Environment.Exit(0);
        }

        private static Process? StartGuardian()
        {
            if (!OperatingSystem.IsWindows()) return null;

            try
            {
                return Process.Start(new ProcessStartInfo(GuardianPath, $"{Environment.ProcessId}")
                {
                    UseShellExecute = false,
                });
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to start Guardian: {ex.Message}");
                return null;
            }
        }

        private static void OnProcessExit()
        {
            if (s_clearupd) return;
            s_proxyService?.Shutdown();
            s_clearupd = true;
        }

        private static void CheckProxy()
        {
            try
            {
                if (GetProxyInfo() is string proxyInfo)
                {
                    Logger.Info($"Your system proxy {proxyInfo}");
                    Logger.Warning("It seems you are using other proxy software.\nClose them first.\nPress any key to continue if resolved.");
                    Console.ReadKey();
                }
            }
            catch (NullReferenceException) { }
        }

        private static string? GetProxyInfo()
        {
            try
            {
                var proxyUri = WebRequest.GetSystemWebProxy()?.GetProxy(new Uri(""));
                return proxyUri is { Host: not "" } ? $"{proxyUri.Host}:{proxyUri.Port}" : null;
            }
            catch { return null; }
        }
    }
}
