using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pesonet.API.Services.IService;
using Request = Pesonet.API.DTOs.Request;

namespace Pesonet.API.Controllers
{
    //[Authorize]
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


        [HttpGet("status")]
        public async Task<IActionResult> Status(string seqNo)
        {
            try
            {
                var result = await _transactionService.Status(seqNo);

                //if (result.Status == "SUCCESS")
                //{
                    return Ok(result);
                //}
                //else
                //{
                //    return BadRequest(result);
                //}

            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

    }
}
