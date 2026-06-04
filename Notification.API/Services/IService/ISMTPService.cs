using Model = Notification.API.Models;
using DTO = Notification.API.DTOs;

namespace Notification.API.Services.IService
{
    public interface ISMTPService
    {
        Task<string> Send(Model.SMTPConfig config, DTO.Request.Mail request, string mailType);
    }
}
