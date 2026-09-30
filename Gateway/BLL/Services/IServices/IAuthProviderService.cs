using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services.IService
{
    public interface IAuthProviderService
    {
        Task<List<Response.FAuthProvider>> Get(string id);
        Task<Response.VAuthProvider> Filter(Request.FParam model);
        Task<Response.Result> Create(Request.AuthProvider model);
        Task<Response.Result> Update(Request.AuthProvider model);
        Task<Response.Result> Delete(Request.AuthProvider model);
        Task<Response.Result> Restore(Request.AuthProvider model);
    }
}