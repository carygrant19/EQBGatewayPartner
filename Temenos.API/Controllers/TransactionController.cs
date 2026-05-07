using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Temenos.API.Sevices.IService;
using Request = Temenos.API.DTOs.Request;

namespace Temenos.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController(ITransactionService transactionService) : ControllerBase
    {
        private readonly ITransactionService _transactionService = transactionService;

        [HttpPost("fundTransfer")]
        public async Task<IActionResult> FundTransfer(string uId, string companyId, Request.Transaction request) 
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

        [HttpDelete("reverse")]
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

        [HttpGet("status")]
        public async Task<IActionResult> Status(string uId)
        {
            try
            {
                var result = await _transactionService.Status(uId);

                if (result.resultMessage == "SUCCESS")
                    return Ok(result.TransactionStatus);
                else
                    return Ok("No record found");

            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

    }
}
