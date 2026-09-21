using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Gateway.BLL.Services
{
    public class OutboundAuthProfileService(EFDbContext dbContext) : IOutboundAuthProfileService
    {
        public async Task<List<OutboundAuthProfile>> GetAllActiveAsync()
        {
            return await dbContext.Set<OutboundAuthProfile>()
                .Where(p => p.IsActive)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}