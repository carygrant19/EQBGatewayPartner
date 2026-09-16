using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services.IServices
{
    public interface IBranchService
    {
        Task<List<Response.Branch>> Get(string branch);
        Task<Response.VBranch> Filter(Request.FParam model);
        Task<Response.Result> Create(Request.Branch model);
        Task<Response.Result> Update(Request.Branch model);
        Task<Response.Result> Delete(Request.Branch model);
        Task<Response.Result> Restore(Request.Branch model);
    }
}
