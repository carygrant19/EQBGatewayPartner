using Gateway.BLL.Services;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Gateway.BLL
{
    public static class PortalServiceExtensions
    {
        public static IServiceCollection PortalServices(this IServiceCollection services)
        { 
            services.AddHttpClient();

            services.AddScoped<ILogService, LogService>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IRouteService, RouteService>();
             

            services.AddScoped<IAuthProviderService, AuthProviderService>();
            services.AddScoped<IOutboundAuthProfileService, OutboundAuthProfileService>();
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ICompanyService, CompanyService>();
            services.AddScoped<ILDAPService, LDAPService>();
            services.AddScoped<IModuleService, ModuleService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IUserService, UserService>();

            return services;
        }
    }
}