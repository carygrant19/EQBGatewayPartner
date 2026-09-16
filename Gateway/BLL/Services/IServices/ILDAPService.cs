using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services.IService
{
    public interface ILDAPService
    { 
        Task<Response.User> Login(Request.User user);
        Task<bool> CheckPasswordExpired(string username, string path);
        Task<Response.User> LDAPUserDetails(string username);

    }
}
