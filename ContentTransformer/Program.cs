using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using Topshelf;

namespace ContentTransformer
{
    internal class Program
    {
        private static int Main()
        {
            TopshelfExitCode exitCode = HostFactory.Run(hostConfigurator =>
            {
                WebConfig config = WebConfig.TryLoadOrNewWebRunnerConfig();

                hostConfigurator.AddCommandLineDefinition("useSSL", useSSLArgument => { config.UseSSL = bool.Parse(useSSLArgument); });
                hostConfigurator.AddCommandLineDefinition("isLocalMode", isLocalModeArgument => { config.IsLocalMode = bool.Parse(isLocalModeArgument); });
                hostConfigurator.AddCommandLineDefinition("port", portArgument => { config.Port = int.Parse(portArgument); });
                hostConfigurator.ApplyCommandLine();

                hostConfigurator.Service<WebLauncher>(s =>
                {
                    s.ConstructUsing(hostSettings => new WebLauncher(config.GetUrl()));

                    s.WhenStarted(o => o.Start());
                    s.WhenStopped(o => o.Stop());

                    s.AfterStartingService(() =>
                    {
                        if (!config.OpenBrowser)
                            return;
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {config.GetUrl()}")
                        {
                            CreateNoWindow = true
                        });
                    });
                });

                hostConfigurator.RunAsNetworkService();
                hostConfigurator.SetServiceName("ContentTransformer");
                hostConfigurator.SetDescription($"Content Transformer Service (v{Assembly.GetExecutingAssembly().GetName().Version})");
                hostConfigurator.SetDisplayName($"Content Transformer - {config.GetUrl()}");
                hostConfigurator.OnException(exception =>
                {
                    Console.Write(exception.Message);
                });
                hostConfigurator.StartAutomatically();
            });

            return (int)exitCode;
        }

        private class WebLauncher
        {
            private IDisposable _app;
            private readonly string _url;

            public WebLauncher(string url)
            {
                _url = url;
            }

            public void Start()
            {
                _app = WebApp.Start<Startup>(_url);
            }
            public void Stop()
            {
                _app.Dispose();
            }
        }
        private class WebConfig
        {
            public WebConfig()
            {
                IsLocalMode = true;
                Port = 5000;
            }

            [JsonProperty("useSSL", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool UseSSL { get; set; }
            [JsonProperty("isLocalMode", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool IsLocalMode { get; set; }
            [JsonProperty("port", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int Port { get; set; }
            [JsonProperty("openBrowser", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool OpenBrowser { get; set; }

            public string GetUrl()
            {
                return $"{(UseSSL ? "https" : "http")}://{(IsLocalMode ? "localhost" : "*")}:{Port}";
            }

            public static WebConfig TryLoadOrNewWebRunnerConfig()
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string configDirectoryName = Path.GetDirectoryName(assembly.Location);
                if (configDirectoryName == null)
                    return new WebConfig();
                string configFileName = Path.Combine(configDirectoryName, $"{Path.GetFileNameWithoutExtension(assembly.Location)}.json");
                if (!File.Exists(configFileName))
                    return new WebConfig();
                string configJsonContent = File.ReadAllText(configFileName);
                return JsonConvert.DeserializeObject<WebConfig>(configJsonContent);
            }
        }
    }
}
