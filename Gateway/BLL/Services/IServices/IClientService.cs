using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;
namespace Gateway.BLL.Services.IService
{
    public interface IClientService
    {
        Task<Response.Client> Authenticate(string id, string password);
        Task<Response.Client> ByApiKey(string key);
        Task<Response.Client> ByUsernameAndPassword(string username, string password);
        Task<List<Response.Client>> GetAll(bool includeDeleted);
        Task<Response.Client> ById(string id);
        Task<Response.VClient> Filter(Request.FParam model);
        Task<Response.Result> Create(Request.Client model);
        Task<Response.Result> Update(Request.Client model);
        Task<Response.Result> Delete(Request.Client model);
        Task<Response.Result> Restore(Request.Client model);


        Task<Response.APISecurityResult> ResetAPIKeySecret(Request.Client model);
        Task<Response.APISecurityResult> ResetPassword(Request.Client model);
    }
}
