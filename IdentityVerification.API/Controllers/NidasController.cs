using IdentityVerification.API.Services;
using IdentityVerification.API.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Request = IdentityVerification.API.DTO.Request;
using Response = IdentityVerification.API.DTO.Response;
using RequestModel = IdentityVerification.API.Models.Request.Nidas;
using ResponseModel = IdentityVerification.API.Models.Response.Nidas;
using ApiResponse = Common.DTOs.Response.Api;


namespace IdentityVerification.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NidasController(INidasService nidasService) : ControllerBase
    {
        private readonly INidasService _nidasService = nidasService;

        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] Request.Nidas.Verify request)
        {
            try
            {
                var result = await _nidasService.Verify(request);

                //if (result.Success)
                //{
                //    return Ok(result);
                //}
                //else
                //{
                //    return BadRequest(result);
                //}

                return Ok();

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
