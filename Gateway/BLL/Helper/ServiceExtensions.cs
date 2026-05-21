using Gateway.BLL.Services.IServices;
using Gateway.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.BLL.Helper
{
    public static class ServiceExtensions
    { 
        public static IServiceCollection HelperServices(this IServiceCollection services, IConfiguration configuration)
        { 
            var mailSettings = configuration.GetSection("MailSettings").Get<MailSettings>()!;

            services.AddSingleton(mailSettings);
            services.AddScoped<ILogService, LogService>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>)); 
            services.AddScoped<IMailerService, MailerService>();

            return services;
        }
    }
}
