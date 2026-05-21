using Gateway.BLL.Services;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gateway.BLL
{
    public static class PortalServiceExtensions
    {
        public static IServiceCollection PortalServices(this IServiceCollection services)
        {
            services.AddScoped<ILogService, LogService>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IBranchService, BranchService>();
            services.AddScoped<ILDAPService, LDAPService>();
            services.AddScoped<IModuleService, ModuleService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IUserService, UserService>();

            //Application
            services.AddScoped<IRouteService, RouteService>();
            return services; 
        }
    }
}
