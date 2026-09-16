using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services.IService
{
    public interface IModuleService
    {
        Task<List<Response.Module>> GetModuleGroup();
        Task<List<Response.Permission>> GetAllPermission();
        Task<List<Response.ModuleAccess>> GetAllParent();
        Task<List<Response.ModuleAccess>> GetUserModuleAccess(string roleModuleCode);
        Task<List<Response.Module>> Get();
        Task<Response.VModule> Filter(Request.FParam model);
        Task<Response.Result> Create(Request.Module model);
        Task<Response.Result> Update(Request.Module model);
        Task<Response.Result> Delete(Request.Module model);
        Task<Response.Result> Restore(Request.Module model);
    }
}
