using Gateway.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services.IService
{
    public interface IRouteService
    {
        Task<Response.Result> GenerateOcelotConfigFile(OcelotConfigFile ocelotConfigFile);
        Task<List<Response.Route>> Get();
        Task<Response.VRoute> Filter(Request.FParam model);
        Task<Response.Result> Create(Request.Route model);
        Task<Response.Result> Update(Request.Route model);
        Task<Response.Result> Delete(Request.Route model);
        Task<Response.Result> Restore(Request.Route model); 
        Task<List<Response.RouteCategory>> GetCategory();
    }
}