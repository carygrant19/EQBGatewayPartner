using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services.IService
{
    public interface IUserService
    {
        Task LogActiveUser(Request.User model);
        Task<Response.VActivityLog> FilterActivityLog(Request.FParam model);
        Task<List<Response.User>> Get();
        Task<int> GetRemainingDaysPasswordExpiry(string username);
        Task<Response.Result> Validate(Request.User model);
        Task<Response.User> ByUsername(string username);
        Task<bool> CheckActiveSession(Request.User User);
        Task<Response.VUser> Filter(Request.FParam model);
        Task<Response.Result> Create(Request.User model);
        Task<Response.Result> Update(Request.User model);
        Task<Response.Result> Delete(Request.User model);
        Task<Response.Result> Restore(Request.User model);
        Task<Response.Result> ChangeProfileImage(Request.User model);
        Task<Response.Result> ChangePassword(Request.UserPassword model);
        Task<bool> CheckSession(Request.User User);
        Task Logout(string userId);
        Task<Response.Result> UnlockUser(Request.User model);
        Task<Response.Result> ResetPassword(DTO.Request.User model);
    }
}
