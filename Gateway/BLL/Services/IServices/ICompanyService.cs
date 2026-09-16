using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services.IService
{
    public interface ICompanyService
    {
        Task<List<Response.Company>> Get(string id);
        Task<Response.VCompany> Filter(Request.FParam model);
        Task<Response.Result> Create(Request.Company model);
        Task<Response.Result> Update(Request.Company model);
        Task<Response.Result> Delete(Request.Company model); 
    }
}
