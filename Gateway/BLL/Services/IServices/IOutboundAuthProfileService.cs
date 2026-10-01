using Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services.IService
{
    public interface IOutboundAuthProfileService
    {
        Task<List<OutboundAuthProfile>> GetAllActiveAsync(); 
        Task<List<OutboundAuthHeader>> GetHeadersByProfileIdAsync(int profileId);
        Task<List<Response.OutboundAuthProfile>> Get(string id);
        Task<Response.VOutboundAuthProfile> Filter(Request.FParam model);
        Task<Response.Result> Create(Request.OutboundAuthProfile model);
        Task<Response.Result> Update(Request.OutboundAuthProfile model);
        Task<Response.Result> Delete(Request.OutboundAuthProfile model);
        Task<Response.Result> Restore(Request.OutboundAuthProfile model);
    }
}