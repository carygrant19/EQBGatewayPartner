using Gateway.BLL.Services.IServices;
using Gateway.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gateway.Web.Services
{
    public class DashboardMetricsWorker(
        IServiceScopeFactory scopeFactory,
        IHubContext<DashboardHub> hubContext) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly IHubContext<DashboardHub> _hubContext = hubContext;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardService>();

                    var metrics = await dashboardService.GetDashboardMetricsAsync(stoppingToken);
                    await _hubContext.Clients.Group("DashboardWatchers").SendAsync("ReceiveMetrics", metrics, stoppingToken);
                }
                catch
                {
                    // Patuloy pa rin ang pag-ikot kahit magka-aberya sa network/DB
                }

                await Task.Delay(3000, stoppingToken);
            }
        }
    }
}