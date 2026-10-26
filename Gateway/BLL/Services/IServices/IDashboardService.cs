using Gateway.BLL.DTO.Response;
using System.Threading;
using System.Threading.Tasks;

namespace Gateway.BLL.Services.IServices
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetDashboardMetricsAsync(CancellationToken cancellationToken = default);
    }
}