using Gateway.Data.Models;

namespace Gateway.BLL.Services.IService
{
    public interface IAuthProviderService
    {
        Task<List<AuthProvider>> GetAllActiveAsync();
    }
}