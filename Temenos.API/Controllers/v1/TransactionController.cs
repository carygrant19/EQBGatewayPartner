using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Temenos.API.Services.IService.v1;
using Request = Temenos.API.DTOs.Request.v1;
using ApiResponse = Common.DTOs.Response.Api;

namespace Temenos.API.Controllers.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TransactionController(ITransactionService transactionService) : ControllerBase
    {
        private readonly ITransactionService _transactionService = transactionService;

        [HttpPost("fundTransfer")]
        public async Task<IActionResult> FundTransfer(string uId, string companyId, Request.FundTransfer request)
        {
            try
            {
                var result = await _transactionService.FundTransfer(uId, companyId, request);

                if (result.Success)
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

                var dtoResponse = new ApiResponse.Response<object>
                {
                    Success = false,
                    Message = "An unexpected system error occurred while processing the request.",
                    Errors =
                    [
                        new ApiResponse.Error
                        {
                            Message =  ex.Message
                        }
                    ]
                };

                return StatusCode(500, dtoResponse);
            }
        }

        [HttpDelete("reverse")]
        public async Task<IActionResult> Reversal(string companyId, string referenceNo)
        {
            try
            {
                var result = await _transactionService.Reversal(companyId, referenceNo);

                if (result.Success)
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
                var dtoResponse = new ApiResponse.Response<object>
                {
                    Success = false,
                    Message = "An unexpected system error occurred while processing the request.",
                    Errors =
                   [
                       new ApiResponse.Error
                        {
                            Message =  ex.Message
                        }
                   ]
                };

                return StatusCode(500, dtoResponse);
            }
        }

        [HttpGet("status")]
        public async Task<IActionResult> Status(string uId)
        {
            try
            {
                var result = await _transactionService.Status(uId);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return NotFound(result);
                }

            }
            catch (Exception ex)
            {
                var dtoResponse = new ApiResponse.Response<object>
                {
                    Success = false,
                    Message = "An unexpected system error occurred while processing the request.",
                    Errors =
                   [
                       new ApiResponse.Error
                        {
                            Message =  ex.Message
                        }
                   ]
                };

                return StatusCode(500, dtoResponse);
            }
        }

        [HttpGet("closingBalance")]
        public async Task<IActionResult> ClosingBalance(string referenceNo)
        {
            try
            {
                var result = await _transactionService.ClosingBalance(referenceNo);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return NotFound(result);
                }

            }
            catch (Exception ex)
            {
                var dtoResponse = new ApiResponse.Response<object>
                {
                    Success = false,
                    Message = "An unexpected system error occurred while processing the request.",
                    Errors =
                   [
                       new ApiResponse.Error
                        {
                            Message =  ex.Message
                        }
                   ]
                };

                return StatusCode(500, dtoResponse);
            }
        }

    }
}
