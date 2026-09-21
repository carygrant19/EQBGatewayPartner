using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Gateway.BLL.Services
{
    public class AuthProviderService(EFDbContext dbContext) : IAuthProviderService
    {
        public async Task<List<AuthProvider>> GetAllActiveAsync()
        {
            return await dbContext.Set<AuthProvider>()
                .Where(p => p.IsActive)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}