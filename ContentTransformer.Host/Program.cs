using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using ContentTransformer.Common;
using Newtonsoft.Json;
using Topshelf;

namespace ContentTransformer.Host
{
    internal class Program
    {
        private static int Main()
        {
            Type launcherType = Type.GetType(ConfigurationManager.AppSettings["launcherType"]);
            if (launcherType == null)
                throw new Exception("The launcher type setting must be defined.");

            TopshelfExitCode exitCode = HostFactory.Run(hostConfigurator =>
            {
                WebApplicationConfig config = WebApplicationConfig.TryLoadOrNewWebRunnerConfig();
                
                hostConfigurator.AddCommandLineDefinition("useSSL", useSSLArgument => { config.UseSSL = bool.Parse(useSSLArgument); });
                hostConfigurator.AddCommandLineDefinition("isLocalMode", isLocalModeArgument => { config.IsLocalMode = bool.Parse(isLocalModeArgument); });
                hostConfigurator.AddCommandLineDefinition("port", portArgument => { config.Port = int.Parse(portArgument); });
                hostConfigurator.ApplyCommandLine();

                hostConfigurator.Service<WebApplicationLauncherAdapter>(s =>
                {
                    s.ConstructUsing(hostSettings => new WebApplicationLauncherAdapter(launcherType));
                    
                    s.WhenStarted(o => o.Start(config));
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
                hostConfigurator.StartAutomatically();

                hostConfigurator.SetServiceName("ContentTransformer");
                hostConfigurator.SetDescription($"Content Transformer Service (v{Assembly.GetExecutingAssembly().GetName().Version})");
                hostConfigurator.SetDisplayName($"Content Transformer - {config.GetUrl()}");
                hostConfigurator.OnException(exception =>
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    while (true)
                    {
                        if (exception == null)
                            break;
                        Console.WriteLine(exception.Message);
                        exception = exception.InnerException;
                    }
                    Console.ResetColor();
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                });
            });

            return (int)exitCode;
        }

        #region Inner Types
        private class WebApplicationLauncherAdapter : IWebApplicationLauncher
        {
            private readonly IWebApplicationLauncher _targetLauncher;

            public WebApplicationLauncherAdapter(Type targetType)
            {
                _targetLauncher = (IWebApplicationLauncher)Activator.CreateInstance(targetType);
            }

            #region Implementation of IWebApplicationLauncher
            public void Start(IWebApplicationConfig configuration)
            {
                _targetLauncher.Start(configuration);
            }
            public void Stop()
            {
                _targetLauncher.Stop();
            }
            #endregion
        }
        private class WebApplicationConfig : IWebApplicationConfig
        {
            public WebApplicationConfig()
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
            public string GetUrl()
            {
                return $"{(UseSSL ? "https" : "http")}://{(IsLocalMode ? "localhost" : "*")}:{Port}";
            }

            [JsonProperty("openBrowser", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool OpenBrowser { get; set; }

            public static WebApplicationConfig TryLoadOrNewWebRunnerConfig()
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string configDirectoryName = Path.GetDirectoryName(assembly.Location);
                if (configDirectoryName == null)
                    return new WebApplicationConfig();
                string configFileName = Path.Combine(configDirectoryName, $"{Path.GetFileNameWithoutExtension(assembly.Location)}.json");
                if (!File.Exists(configFileName))
                    return new WebApplicationConfig();
                string configJsonContent = File.ReadAllText(configFileName);
                return JsonConvert.DeserializeObject<WebApplicationConfig>(configJsonContent);
            }
        }
        #endregion
    }
}
