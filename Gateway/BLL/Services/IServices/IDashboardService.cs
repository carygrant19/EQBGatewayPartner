using Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services.IService
{
    public interface IDashboardService
    {
        Task<DashboardSummary> GetDashboardMetricsAsync();
    }
}