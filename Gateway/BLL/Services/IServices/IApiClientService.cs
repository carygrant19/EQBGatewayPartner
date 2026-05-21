using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;
namespace Gateway.BLL.Services.IService
{
    public interface IApiClientService
    {
        Task<Response.ApiClient> Authenticate(string id, string password);
        Task<Response.ApiClient> ByApiKey(string key);
        Task<List<Response.ApiClient>> GetAll(bool includeDeleted);
        Task<Response.ApiClient> ById(string id);
        Task<Response.VApiClient> Filter(Request.FParam model);
        Task<Response.Result> Create(Request.ApiClient model);
        Task<Response.Result> Update(Request.ApiClient model);
        Task<Response.Result> Delete(Request.ApiClient model);


        Task<Response.APISecurityResult> ResetAPIKeySecret(Request.ApiClient model);
        Task<Response.APISecurityResult> ResetPassword(Request.ApiClient model);
    }
}
