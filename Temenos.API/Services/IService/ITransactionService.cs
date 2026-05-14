using Request = Temenos.API.DTOs.Request;
using Response = Temenos.API.DTOs.Response;

namespace Temenos.API.Services.IService
{
    public interface ITransactionService
    {
        Task<Response.Transaction> FundTransfer(string uId, string companyId, Request.Transaction request);
        Task<Response.Transaction> Reversal(string companyId, string referenceNo);
        Task<(Response.TransactionStatus? TransactionStatus, string resultMessage)> Status(string uId);
    }
}
