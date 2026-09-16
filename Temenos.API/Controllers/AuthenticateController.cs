using Common.Services.IService;
using Microsoft.AspNetCore.Mvc;
using Request = Common.DTOs.Request;

namespace Temenos.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticateController(IAuthenticateService authenticateService) : ControllerBase
    {
        private readonly IAuthenticateService _authenticateService = authenticateService;

        [HttpPost]
        public async Task<IActionResult> Authenticate(
            [FromBody] Request.Authenticate model,
            [FromHeader] Request.VendorHeaderRequest headers)
        {
            try
            {
                var result = await _authenticateService.Authenticate(headers, model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}