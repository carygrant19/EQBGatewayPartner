using Request = IdentityVerification.API.DTO.Request;

namespace IdentityVerification.API.Services.IService
{
    public interface INidasService
    {
        Task<(int StatusCode, string ResponseBody, Dictionary<string, string[]> Headers)> Verify(Request.VerifyRequest dtoRequest);
        Task<(int StatusCode, string ResponseBody, Dictionary<string, string[]> Headers)> QRQueryCheck(Request.QrCheckRequest dtoRequest);
        Task<(int StatusCode, string ResponseBody, Dictionary<string, string[]> Headers)> QRQueryVerify(Request.QrVerifyRequest dtoRequest);
    }
}