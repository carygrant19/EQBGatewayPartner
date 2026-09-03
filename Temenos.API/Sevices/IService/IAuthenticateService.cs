using Request = Temenos.API.DTOs.Request;
using Response = Temenos.API.DTOs.Response;

namespace Temenos.API.Sevices.IService
{
    public interface IAuthenticateService
    {
        Task<Response.Authenticate> Authenticate(Request.VendorHeaderRequest headers, Request.Authenticate model);
    }
}
