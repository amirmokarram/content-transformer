using System;

namespace ContentTransformer.Common
{
    public interface IWebApplicationLauncher
    {
        void Start(IWebApplicationConfig configuration);
        void Stop();
    }
}
