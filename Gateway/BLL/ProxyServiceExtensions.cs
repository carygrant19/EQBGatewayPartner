using Gateway.BLL.Services;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gateway.BLL
{
    public static class ProxyServiceExtensions
    {
        public static IServiceCollection ProxyServices(this IServiceCollection services)
        {
            services.AddScoped<ILogService, LogService>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            
            services.AddScoped<IApiClientService, ApiClientService>();
            services.AddScoped<ICertificateValidatorService, CertificateValidatorService>();
            services.AddSingleton<IMailerService, MailerService>();
            services.AddScoped<IUserService, UserService>();
            return services; 
        }
    }
}
