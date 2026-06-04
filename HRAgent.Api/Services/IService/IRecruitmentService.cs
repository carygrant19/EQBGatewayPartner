using DTO = HRAgent.Api.DTOs.Recruitment;

namespace HRAgent.Api.Services.IService
{
    public interface IRecruitmentService
    {
        Task<DTO.Response.Evaluate?> EvaluateCandidate(DTO.Request.Evaluate request);
    }
}
