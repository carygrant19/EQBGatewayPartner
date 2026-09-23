using Gateway.Data.Models;

namespace Gateway.BLL.Services.IService
{
    public interface IOutboundAuthProfileService
    {
        Task<List<OutboundAuthProfile>> GetAllActiveAsync();

        Task<List<OutboundAuthHeader>> GetHeadersByProfileIdAsync(int profileId);
    }
}