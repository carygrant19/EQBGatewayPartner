using Microsoft.AspNetCore.Mvc;
using HRAgent.Api.Services.IService;
using DTO = HRAgent.Api.DTOs.Assistant;

namespace HRAgent.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssistantController(IAssistantService assistantService) : ControllerBase
    {
        private readonly IAssistantService _assistantService = assistantService;
 
        [HttpPost("policy")]
        public async Task<IActionResult> Policy(DTO.Request.Chat request)
        {
            try
            {
                var result = await _assistantService.Chat(request);

                return Ok(result);

            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
