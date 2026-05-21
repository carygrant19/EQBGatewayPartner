using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services.IService
{
    public interface IPermissionService
    {
        Task<List<Response.Permission>> Get();
        Task<Response.VPermission> Filter(Request.FParam model);
        Task<Response.Result> Create(Request.Permission model);
        Task<Response.Result> Update(Request.Permission model);
        Task<Response.Result> Delete(Request.Permission model);
        Task<Response.Result> Restore(Request.Permission model);
    }
}
