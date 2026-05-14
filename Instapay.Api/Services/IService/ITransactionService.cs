using Request = Instapay.Api.DTOs.Request;
using Response = Instapay.Api.DTOs.Response;

namespace Instapay.Api.Services.IService
{
    public interface ITransactionService
    {
        Task<Response.Transaction> FundTransfer(string uId, Request.Transaction request);
    }

}
