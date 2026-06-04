using Microsoft.AspNetCore.Mvc;
using HRAgent.Api.Services.IService;
using DTO =  HRAgent.Api.DTOs.Recruitment;

namespace HRAgent.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecruitmentController(IRecruitmentService recrutimentService) : ControllerBase
    {
        private readonly IRecruitmentService _recrutimentService = recrutimentService;

        [HttpPost("evaluateCandidate")]
        public async Task<IActionResult> EvaluateCandidate(DTO.Request.Evaluate request)
        {
            try
            {
                var result = await _recrutimentService.EvaluateCandidate(request);

                return Ok(result);

            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
