using Gateway.BLL.Services;
using Gateway.Helper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gateway.BLL
{
    public static class ServiceExtensions
    {
        public static IServiceCollection GatewayServices(this IServiceCollection services)
        {
            services.AddScoped<ILogService, LogService>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

             
            return services; 
        }
    }
}
