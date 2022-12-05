using System;
using ValmontApp.Common;
using Microsoft.Extensions.DependencyInjection;

namespace ValmontApp.FunctionApps
{
    public class IoCContainer
    {
        private static IServiceProvider _provider;

        public static IServiceProvider Create()
        {
            return _provider ?? (_provider = ConfigureServices());
        }

        private static IServiceProvider ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection();
            var settings = new Settings(s => Environment.GetEnvironmentVariable(s, EnvironmentVariableTarget.Process));
            services.AddSingleton<ISettings>(settings);
            services.AddTransient<IAzureTableRepository>(s => new AzureTableRepository(settings.FunctionStorageConnectionString));
            return services.BuildServiceProvider();
        }
    }
}
