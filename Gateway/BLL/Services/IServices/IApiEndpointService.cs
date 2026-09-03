using Gateway.BLL.DTO.Request;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services.IService
{
    public interface IApiEndpointService
    {
        Task<List<Response.ApiEndpoint>> GetActiveEndpointsAsync();
        Task<Response.VApiEndpoint> FilterAsync(FParam model);
        Task<Response.Result> CreateAsync(Request.ApiEndpoint model);
        Task<Response.Result> UpdateAsync(Request.ApiEndpoint model);
        Task<Response.Result> DeleteAsync(Request.ApiEndpoint model);
        Task<Response.Result> RestoreAsync(Request.ApiEndpoint model);
    }
}