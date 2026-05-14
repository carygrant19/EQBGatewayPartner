using Request = Common.DTOs.Request;
using Response = Common.DTOs.Response;

namespace Common.Services.IService
{
    public interface IAuthenticateService
    {
        Task<Response.Authenticate> Authenticate(Request.Authenticate model);
    }
}
