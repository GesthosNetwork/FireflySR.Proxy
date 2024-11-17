namespace FireflySR.Proxy
{
    using System;
    using System.Net;
    using System.Net.Security;
    using System.Text;
    using System.Threading.Tasks;
    using Titanium.Web.Proxy;
    using Titanium.Web.Proxy.EventArguments;
    using Titanium.Web.Proxy.Models;

    internal class ProxyService
    {
        private readonly ProxyConfig _conf;
        private readonly ProxyServer _webProxyServer;
        private readonly string _targetRedirectHost;
        private readonly int _targetRedirectPort;

        public ProxyService(string targetRedirectHost, int targetRedirectPort, ProxyConfig conf)
        {
            _conf = conf ?? throw new ArgumentNullException(nameof(conf));
            _webProxyServer = new ProxyServer();
            _webProxyServer.CertificateManager.EnsureRootCertificate(true, true, false);

            _webProxyServer.BeforeRequest += BeforeRequest;
            _webProxyServer.ServerCertificateValidationCallback += OnCertValidation;

            _targetRedirectHost = targetRedirectHost;
            _targetRedirectPort = targetRedirectPort;

            int port = conf.ProxyBindPort == 0 ? Random.Shared.Next(10000, 60000) : conf.ProxyBindPort;
            SetEndPoint(new ExplicitProxyEndPoint(IPAddress.Any, port, true));
        }

        private void SetEndPoint(ExplicitProxyEndPoint explicitEP)
        {
            explicitEP.BeforeTunnelConnectRequest += BeforeTunnelConnectRequest;

            _webProxyServer.AddEndPoint(explicitEP);
            _webProxyServer.Start();

            if (OperatingSystem.IsWindows())
            {
                _webProxyServer.SetAsSystemHttpProxy(explicitEP);
                _webProxyServer.SetAsSystemHttpsProxy(explicitEP);
            }
        }

        public void Shutdown()
        {
            _webProxyServer?.Stop();
            _webProxyServer?.Dispose();
        }

        private Task BeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs args)
        {
            string hostname = args.HttpClient.Request.RequestUri.Host;
            args.DecryptSsl = ShouldRedirect(hostname);

            return Task.CompletedTask;
        }

        private Task OnCertValidation(object sender, CertificateValidationEventArgs args)
        {
            if (args.SslPolicyErrors == SslPolicyErrors.None)
                args.IsValid = true;

            return Task.CompletedTask;
        }

        private bool ShouldForceRedirect(string path)
        {
            foreach (var keyword in _conf.ForceRedirectOnUrlContains)
            {
                if (path.Contains(keyword)) return true;
            }
            return false;
        }

        private bool ShouldBlock(Uri uri)
        {
            var path = uri.AbsolutePath;
            return _conf.BlockUrls.Contains(path);
        }

        private Task BeforeRequest(object sender, SessionEventArgs args)
        {
            string hostname = args.HttpClient.Request.RequestUri.Host;
            if (ShouldRedirect(hostname) || ShouldForceRedirect(args.HttpClient.Request.RequestUri.AbsolutePath))
            {
                string requestUrl = args.HttpClient.Request.RequestUri.ToString();
                Uri local = new Uri($"http://{_targetRedirectHost}:{_targetRedirectPort}/");

                Uri builtUrl = new UriBuilder(requestUrl)
                {
                    Scheme = local.Scheme,
                    Host = local.Host,
                    Port = local.Port
                }.Uri;

                string replacedUrl = builtUrl.ToString();

                if (ShouldBlock(builtUrl))
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("[Blocked]: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(requestUrl);
                    Console.ResetColor();
                    Console.WriteLine();

                    args.Respond(new Titanium.Web.Proxy.Http.Response(Encoding.UTF8.GetBytes("Access denied"))
                    {
                        StatusCode = 404,
                        StatusDescription = "Resource Blocked",
                    }, true);
                    return Task.CompletedTask;
                }

                args.HttpClient.Request.Url = replacedUrl;

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write("Redirecting: ");
                Console.ResetColor();
                Console.WriteLine(requestUrl);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("=>");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(replacedUrl);
                Console.ResetColor();
                Console.WriteLine();
            }

            return Task.CompletedTask;
        }

        private bool ShouldRedirect(string hostname)
        {
            if (hostname.Contains(':'))
                hostname = hostname[..hostname.IndexOf(':')];

            foreach (string domain in _conf.AlwaysIgnoreDomains)
            {
                if (hostname.EndsWith(domain))
                {
                    return false;
                }
            }

            foreach (string domain in _conf.RedirectDomains)
            {
                if (hostname.EndsWith(domain))
                    return true;
            }

            return false;
        }
    }
}