using System;
using ContentTransformer.Common;
using Microsoft.Owin.Hosting;

namespace ContentTransformer
{
    internal class WebApplicationLauncher : IWebApplicationLauncher
    {
        private IDisposable _app;

        #region Implementation of IWebApplicationLauncher
        public void Start(IWebApplicationConfig configuration)
        {
            string url = $"{(configuration.UseSSL ? "https" : "http")}://{(configuration.IsLocalMode ? "localhost" : "*")}:{configuration.Port}";
            _app = WebApp.Start<Startup>(url);
        }
        public void Stop()
        {
            _app.Dispose();
        }
        #endregion
    }
}
