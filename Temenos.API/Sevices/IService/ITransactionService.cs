using Request = Temenos.API.DTOs.Request;
using Response = Temenos.API.DTOs.Response;

namespace Temenos.API.Sevices.IService
{
    public interface ITransactionService
    {
        Task<Response.Transaction> FundTransfer(string uId, string companyId, Request.Transaction request);
        Task<Response.Transaction> Reversal(string companyId, string referenceNo);
    }
}
