using Request = Pesonet.API.DTOs.Request;
using Response = Pesonet.API.DTOs.Response;

namespace Pesonet.API.Services.IService
{
    public interface ITransactionService
    {
        Task<Response.Transaction> FundTransfer(string uId, Request.Transaction request);
        Task<Response.Status?> Status(string seqNo);
    }

 
}
