using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Temenos.API.Services.IService.v1.loans;
using ApiResponse = Common.DTOs.Response.Api;

namespace Temenos.API.Controllers.v1.loans
{
    [Route("api/v1/loans/[controller]")]
    [ApiController]
    public class AccountController(IAccountService accountService) : ControllerBase
    {
        private readonly IAccountService _accountService = accountService;

        [HttpGet("amortizationSchedule")]
        public async Task<IActionResult> AmortizationSchedule([FromQuery]string arrangementId)
        {
            try
            {
                var result = await _accountService.AmortizationSchedule(arrangementId);

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
