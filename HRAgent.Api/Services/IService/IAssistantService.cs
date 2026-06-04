using DTO = HRAgent.Api.DTOs.Assistant;

namespace HRAgent.Api.Services.IService
{
    public interface IAssistantService
    {
        Task<string?> Chat(DTO.Request.Chat request);
    }
}
