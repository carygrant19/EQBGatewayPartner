using Microsoft.AspNetCore.Mvc;
using Notification.API.Services.IService;
using Request = Notification.API.DTOs.Request;
using Model = Notification.API.Models;

namespace Notification.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SMTPController : ControllerBase
    {
        private readonly Model.SMTPConfig _smtpConfigs = new();
        private readonly ISMTPService _smtpService;
        public SMTPController(IConfiguration configuration, ISMTPService sMTPService)
        {
            _smtpService = sMTPService;
            configuration.GetSection("SMTPConfig").Bind(_smtpConfigs);
        }

        [HttpPost("notification")]
        public async Task<IActionResult> Notification(Request.Mail request)
        {
            try
            {
               string type = "NOTIF";

               var result = await _smtpService.Send(_smtpConfigs, request, type);


                if (result.Equals("SUCCESS", StringComparison.CurrentCultureIgnoreCase))
                {
                    return Ok("Email sent.");
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("alert")]
        public async Task<IActionResult> Alert(Request.Mail request)
        {
            try
            {
                string type = "ALERT";

                var result = await _smtpService.Send(_smtpConfigs, request, type);


                if (result.Equals("SUCCESS", StringComparison.CurrentCultureIgnoreCase))
                {
                    return Ok("Email sent.");
                }
                else
                {
                    return BadRequest(result);
                }

            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
