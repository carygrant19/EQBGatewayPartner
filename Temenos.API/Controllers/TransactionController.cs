using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Temenos.API.Sevices.IService;
using Request = Temenos.API.DTOs.Request;

namespace Temenos.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController(ITransactionService transactionService) : ControllerBase
    {
        private readonly ITransactionService _transactionService = transactionService;

        [HttpPost("fundTransfer")]
        public async Task<IActionResult> FundTransfer([FromHeader]string uId, [FromHeader]string companyId, [FromBody] Request.Transaction request) 
        {
            try
            {
                var result = await _transactionService.FundTransfer(uId, companyId, request);

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

        [HttpPost("reverse")]
        public async Task<IActionResult> Reversal(string companyId, string referenceNo)
        {
            try
            {
                var result = await _transactionService.Reversal(companyId, referenceNo);

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
