using Microsoft.Win32;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

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
            Console.Title = Title;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(" ___ ___ ___ ___ ___ _ __   __  ___ ___    ___ ___  _____  ____   __");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("| __|_ _| _ \\ __| __| |\\ \\ / / / __| _ \\  | _ \\ _ \\/ _ \\ \\/ /\\ \\ / /");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine("| _| | ||   / _|| _|| |_\\ V /  \\__ \\   /  |  _/   / (_) >  <  \\ V / ");
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.WriteLine("|_| |___|_|_\\___|_| |____|_|   |___/_|_\\  |_| |_|_\\\\___/_/\\_\\  |_|  ");
			Console.ResetColor();
			Console.WriteLine();
            _ = Task.Run(WatchGuardianAsync);
            CheckProxy();
            InitConfig();

            var conf = JsonSerializer.Deserialize<ProxyConfig>(File.ReadAllText(ConfigPath))
                       ?? throw new FileLoadException("Please correctly configure config.json.");
            s_proxyService = new ProxyService(conf.DestinationHost, conf.DestinationPort, conf);
            Console.WriteLine("Proxy now running");
			Console.WriteLine("");

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Console.CancelKeyPress += OnProcessExit;

            await Task.Delay(-1);
        }

        private static async Task WatchGuardianAsync()
        {
            var proc = StartGuardian();
            if (proc == null)
            {
                Console.WriteLine("Guardian start failed. Your proxy settings may not be able to recover after closing.");
                return;
            }

            while (!proc.HasExited)
            {
                await Task.Delay(1000);
            }
            Console.WriteLine("! Guardian exit");
            OnProcessExit(null, null);
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
                Console.WriteLine(ex);
                return null;
            }
        }

        private static void InitConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                File.Copy(ConfigPath, ConfigPath);
            }
        }

        private static void OnProcessExit(object? sender, EventArgs? args)
        {
            if (s_clearupd) return;
            s_proxyService?.Shutdown();
            s_clearupd = true;
        }

        public static void CheckProxy()
        {
            try
            {
                string? proxyInfo = GetProxyInfo();
                if (proxyInfo != null)
                {
					Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("It seems you are using other proxy software (such as Fiddler, MITMProxy, etc).");
                    Console.WriteLine($"Your system proxy: {proxyInfo}");
                    Console.WriteLine("You have to close all other proxy software to ensure FireflySR.Proxy works well.");
                    Console.WriteLine("Press any key to continue if you closed other proxy software, or you think you are not using other proxy.");
                    Console.ReadKey();
					Console.ResetColor();
                }
            }
            catch (NullReferenceException)
            {}
        }

        public static string? GetProxyInfo()
        {
            try
            {
                IWebProxy proxy = WebRequest.GetSystemWebProxy();
                Uri? proxyUri = proxy.GetProxy(new Uri("https://www.example.com"));
                if (proxyUri == null) return null;

                return $"{proxyUri.Host}:{proxyUri.Port}";
            }
            catch
            {
                return null;
            }
        }
    }
}
