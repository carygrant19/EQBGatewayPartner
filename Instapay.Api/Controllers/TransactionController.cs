using Instapay.Api.Services.IService;
using Microsoft.AspNetCore.Mvc;
using Request = Instapay.Api.DTOs.Request;

namespace Instapay.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController(ITransactionService transactionService) : ControllerBase
    {
        private readonly ITransactionService _transactionService = transactionService;

        [HttpPost("fundTransfer")]
        public async Task<IActionResult> FundTransfer(string uId, Request.Transaction request)
        {
            try
            {
                var result = await _transactionService.FundTransfer(uId, request);

                if (result.Status == "SUCCESS")
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }

            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
