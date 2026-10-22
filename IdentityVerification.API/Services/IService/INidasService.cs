using Request = IdentityVerification.API.DTO.Request;
using Response = IdentityVerification.API.DTO.Response;
using ApiResponse = Common.DTOs.Response.Api;

namespace IdentityVerification.API.Services.IService
{
    public interface INidasService
    {
        Task<object> Verify(Request.Nidas.Verify request);

    }
}
