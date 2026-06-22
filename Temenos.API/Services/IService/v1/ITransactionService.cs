using Request = Temenos.API.DTOs.Request.v1;
using Response = Temenos.API.DTOs.Response.v1;
using ApiResponse = Common.DTOs.Response.Api;

namespace Temenos.API.Services.IService.v1
{
    public interface ITransactionService
    {
        Task<ApiResponse.Response<Response.FundTransfer>> FundTransfer(string uId, string companyId, Request.FundTransfer request);
        Task<ApiResponse.Response<object>> Reversal(string companyId, string referenceNo);
        Task<ApiResponse.Response<Response.TransactionStatus>>  Status(string uId);
        Task<ApiResponse.Response<object>> ClosingBalance(string referenceNo);
    }
}
