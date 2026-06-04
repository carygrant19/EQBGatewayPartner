using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Notification.API.Services;
using Notification.API.Services.IService;
using Model = Notification.API.Models;
using Request = Notification.API.DTOs.Request;

namespace Notification.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SMSController : ControllerBase
    {
        private readonly List<Model.SMSConfig> _smsConfigs = [];
        private readonly ISMSService _smsService;
        public SMSController(IConfiguration configuration, ISMSService smsService)
        {
            _smsService = smsService;
            configuration.GetSection("SMSConfig").Bind(_smsConfigs);
        }

        [HttpPost("notification")]
        public async Task<IActionResult> Notification(Request.Sms request)
        {
            try
            {
                string type = "NOTIF";
                var config = _smsConfigs.FirstOrDefault(a => a.Type.Equals(type, StringComparison.CurrentCultureIgnoreCase));

                var result = await _smsService.Send(config!, request);


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
        public async Task<IActionResult> Alert(Request.Sms request)
        {
            try
            {
                string type = "ALERT";

                var config = _smsConfigs.FirstOrDefault(a => a.Type.Equals(type, StringComparison.CurrentCultureIgnoreCase));

                var result = await _smsService.Send(config!, request);


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
