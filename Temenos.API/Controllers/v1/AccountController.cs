using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Temenos.API.Services.IService.v1;
using ApiResponse = Common.DTOs.Response.Api;
using Request = Temenos.API.DTOs.Request.v1;

namespace Temenos.API.Controllers.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AccountController(IAccountService accountService) : ControllerBase
    {
        private readonly IAccountService _accountService = accountService;

        [HttpGet("details")]
        public async Task<IActionResult> Details(string accountNo)
        {
            try
            {
                var result = await _accountService.Details(accountNo);

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

        [HttpGet("inquireBalance")]
        public async Task<IActionResult> InquireBalance(string accountNo)
        {
            try
            {
                var result = await _accountService.InquireBalance(accountNo);

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
