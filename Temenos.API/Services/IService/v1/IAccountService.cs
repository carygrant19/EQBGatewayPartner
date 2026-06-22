using Request = Temenos.API.DTOs.Request.v1;
using Response = Temenos.API.DTOs.Response.v1;
using ApiResponse = Common.DTOs.Response.Api;

namespace Temenos.API.Services.IService.v1
{
    public interface IAccountService
    {
        Task<ApiResponse.Response<Response.AccountDetails>> Details(string accountNo);
        Task<ApiResponse.Response<Response.BalanceInquiry>> InquireBalance(string accountNo);
    }
}
