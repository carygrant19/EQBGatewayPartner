using Microsoft.AspNetCore.Http;
using Model = Gateway.Data.Models;
namespace Gateway.BLL.Services.IService
{
    public interface IAuthService
    {
        Task<(bool IsSuccess, string JsonResponse, int StatusCode)> ProcessInternalAuthAsync(HttpContext context, Model.Route endpoint);
    }
}