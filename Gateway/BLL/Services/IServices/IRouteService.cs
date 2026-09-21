using Gateway.BLL.DTO.Request;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services.IService
{
    public interface IRouteService
    {
        Task<List<Response.Route>> GetActiveEndpointsAsync();
        Task<Response.VRoute> FilterAsync(FParam model);
        Task<Response.Result> CreateAsync(Request.Route model);
        Task<Response.Result> UpdateAsync(Request.Route model);
        Task<Response.Result> DeleteAsync(Request.Route model);
        Task<Response.Result> RestoreAsync(Request.Route model);
    }
}