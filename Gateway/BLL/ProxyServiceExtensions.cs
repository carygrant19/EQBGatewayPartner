using Gateway.BLL.Services;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Gateway.Proxy.Helper;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.BLL
{
    public static class ProxyServiceExtensions
    {
        public static IServiceCollection ProxyServices(this IServiceCollection services)
        { 
            services.AddHttpClient();

            services.AddMemoryCache();
            services.AddScoped<ILogService, LogService>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IRouteService, RouteService>();
            services.AddScoped<IAuthService, AuthService>();

            services.AddSingleton<DatabaseProxyConfigProvider>();
            services.AddSingleton<IProxyConfigProvider>(sp => sp.GetRequiredService<DatabaseProxyConfigProvider>());

            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<ICertificateValidatorService, CertificateValidatorService>();
            services.AddSingleton<IMailerService, MailerService>();
            services.AddScoped<IUserService, UserService>();

            return services;
        }
    }
}