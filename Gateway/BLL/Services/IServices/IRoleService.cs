using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services.IService
{
    public interface IRoleService
    {
        Task<List<Response.RoleModule>> GetRoleModules(string username);
        Task<Response.VRole> Get();
        Task<List<Response.Module>> ById(string id);
        Task<Response.VRole> Filter(Request.FParam model);
        Task<Response.Result> Create(Request.Role model);
        Task<Response.Result> Update(Request.Role model);
        Task<Response.Result> Delete(Request.Role model);
        Task<Response.Result> Restore(Request.Role model);
    }
}
