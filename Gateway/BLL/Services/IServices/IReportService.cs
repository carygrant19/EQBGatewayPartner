using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services.IServices
{
    public interface IReportService
    {
        Task<List<Response.ReportRouteOption>> GetRoutes();
        Task<List<string>> GetClients();
        Task<Response.VTransactionReport> Filter(Request.FParam model);
        Task<byte[]> ExportCsv(Request.FParam model);
    }
}