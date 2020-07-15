using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using ContentTransformer.Common;
using ContentTransformer.Common.Services.ModuleDiscovery;
using ContentTransformer.Services;
using ContentTransformer.Services.ModuleDiscovery;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.StaticFiles.ContentTypes;
using Owin;
using Unity;
using Unity.WebApi;

namespace ContentTransformer
{
    internal class Startup
    {
        private static IUnityContainer _unityContainer;

        // ReSharper disable once UnusedMember.Global
        public void Configuration(IAppBuilder app)
        {
            HttpConfiguration httpConfiguration = new HttpConfiguration();

            httpConfiguration.Services.Replace(typeof(IAssembliesResolver), new CustomAssemblyResolver());
            httpConfiguration.MapHttpAttributeRoutes();
            
            #region Create WWW
            const string rootName = "www";
            string rootPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(), rootName);
            if (!Directory.Exists(rootPath))
                Directory.CreateDirectory(rootPath);
            #endregion

            #region Configure WebAPI
#if DEBUG
            app.UseErrorPage();
#endif
            app.UseWebApi(httpConfiguration);
            #endregion

            #region Configure Static Files
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.Value.Contains(".json"))
                    context.Request.Path = new PathString($"/{rootName}{context.Request.Path.Value}");
                await next.Invoke();
            });

            StaticFileOptions staticFileOptions = new StaticFileOptions
            {
                RequestPath = new PathString($"/{rootName}"),
                FileSystem = new PhysicalFileSystem(rootPath)
            };
            ((FileExtensionContentTypeProvider)staticFileOptions.ContentTypeProvider).Mappings.Add(".json", "application/json");
            app.UseStaticFiles(staticFileOptions);
            #endregion

            #region Configure File Server
            PhysicalFileSystem physicalFileSystem = new PhysicalFileSystem(rootPath);
            FileServerOptions fileServerOptions = new FileServerOptions
            {
                EnableDefaultFiles = true,
                FileSystem = physicalFileSystem
            };
            app.UseFileServer(fileServerOptions);
            #endregion

            #region Configure Unity
            _unityContainer = new UnityContainer();
            httpConfiguration.DependencyResolver = new UnityDependencyResolver(_unityContainer);

            //Register Controllers
            Type controllerType = typeof(ApiController);
            Type[] types = Assembly.GetExecutingAssembly().GetTypes().Where(x => controllerType.IsAssignableFrom(x)).ToArray();
            foreach (Type type in types)
                _unityContainer.RegisterType(type);

            _unityContainer.RegisterSingleton<IModuleDiscoveryService, ModuleDiscoveryService>();
            #endregion

            _unityContainer.Resolve<IModuleDiscoveryService>().DiscoverModules();

            httpConfiguration.EnsureInitialized();
        }

        #region Inner Type

        private class CustomAssemblyResolver : IAssembliesResolver
        {
            public ICollection<Assembly> GetAssemblies()
            {
                return new List<Assembly>
                {
                    Assembly.GetExecutingAssembly()
                };
            }
        }

        #endregion
    }
}