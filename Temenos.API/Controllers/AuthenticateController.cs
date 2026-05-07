using Microsoft.AspNetCore.Mvc;
using Temenos.API.Sevices.IService;
using Request = Temenos.API.DTOs.Request;

namespace Temenos.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticateController(IAuthenticateService authenticateService) : ControllerBase
    {
        private readonly IAuthenticateService _authenticateService = authenticateService;
        
        [HttpPost]
        [Route("")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> Authenticate(Request.Authenticate model)
        {
            try
            {
                return Ok(await _authenticateService.Authenticate(model));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
