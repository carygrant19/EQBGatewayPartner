using Model = Notification.API.Models;
using DTO = Notification.API.DTOs;

namespace Notification.API.Services.IService
{
    public interface ISMSService
    {
        Task<string> Send(Model.SMSConfig config, DTO.Request.Sms request);
    }
}
