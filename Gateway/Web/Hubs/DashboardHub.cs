using Gateway.BLL.DTO.Response;
using Gateway.BLL.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Gateway.Web.Hubs
{
    [AllowAnonymous]
    public class DashboardHub(IDashboardService dashboardService) : Hub
    {
        private readonly IDashboardService _dashboardService = dashboardService;

        public async Task JoinDashboard()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "DashboardWatchers");
        }

        public async Task LeaveDashboard()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "DashboardWatchers");
        }

        public async Task<DashboardSummaryDto> GetInitialMetrics()
        {
            return await _dashboardService.GetDashboardMetricsAsync();
        }
    }
}