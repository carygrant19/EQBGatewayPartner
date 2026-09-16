using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services.IService
{
    public interface ICategoryService
    {
        Task<List<Response.Category>> GetAllActiveAsync();
    }
}