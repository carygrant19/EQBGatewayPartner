using Common.Handlers;
using Common.Services;
using Common.Services.IService;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Services
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddOutboundTransactionLogging(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ITransactionLogService, TransactionLogService>();
            services.AddTransient<OutboundLoggingHandler>();
            return services;
        }
    }
}