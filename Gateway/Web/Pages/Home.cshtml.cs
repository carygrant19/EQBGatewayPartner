using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Model = Gateway.Data.Models;

namespace Gateway.Web.Pages
{
    public class HomeModel(
        IConfiguration configuration,
        IUserService userService,
        IDashboardService dashboardService) : PageModelExtension
    {
        private readonly IUserService _userService = userService;
        private readonly IDashboardService _dashboardService = dashboardService;
        private readonly Model.SystemParameters _systemParameters = configuration.GetSection("SystemParameters").Get<Model.SystemParameters>()!;

        [BindProperty]
        public string JavascriptToRun { get; set; } = string.Empty;
        public double SessionTimeoutMinutes { get; set; }
        public double WarningBeforeMinutes { get; set; }
        public string AspireDashboardUrl { get; set; } = string.Empty;

        public async Task<IActionResult> OnGet()
        {
            string page = ValidateAccess();

            if (page == "")
            {
                AspireDashboardUrl = configuration["Telemetry:AspireDashboardUrl"] ?? "http://localhost:18888";

                if (!Convert.ToBoolean(HttpContext.Session.GetObject<bool>("LDAPAuthentication")))
                {
                    var remainingDays = await _userService.GetRemainingDaysPasswordExpiry(Convert.ToString(HttpContext.Session.GetString("Username")!));
                    if (remainingDays >= 0 && _systemParameters.NotifyPasswordExpiryDays >= remainingDays)
                    {
                        JavascriptToRun = "PasswordExpiryNotification(" + remainingDays + ")";
                    }
                }
                return Page();
            }
            else
            {
                return Redirect(page);
            }
        }

        public async Task<IActionResult> OnGetMetrics(CancellationToken cancellationToken)
        {
            var data = await _dashboardService.GetDashboardMetricsAsync(cancellationToken);
            return new JsonResult(data);
        }

        public async Task<IActionResult> OnGetTraceDetails(string traceId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(traceId))
                return BadRequest("TraceId is required.");

            var trace = await _dashboardService.GetTraceTimelineAsync(traceId, cancellationToken);
            if (trace == null)
                return NotFound("Trace timeline not found in audit logs.");

            return new JsonResult(trace);
        }

        public async Task<JsonResult> OnGetSessionConfig()
        {
            SessionTimeoutMinutes = (double)_systemParameters.SessionTimeOutMinutes;
            WarningBeforeMinutes = 2;

            return new JsonResult(new
            {
                TimeoutMinutes = SessionTimeoutMinutes,
                WarningBeforeMinutes = WarningBeforeMinutes
            });
        }

        public JsonResult OnGetSessionExpired()
        {
            try
            {
                var userRequest = new Gateway.BLL.DTO.Request.User()
                {
                    Id = Convert.ToString(HttpContext.Session.GetString("UserId"))!,
                    Username = Convert.ToString(HttpContext.Session.GetString("Username"))!,
                    OpUser = HttpContext.Session.GetString("Username"),
                    OpUserId = Convert.ToString(HttpContext.Session.GetString("UserId")),
                    Terminal = Convert.ToString(HttpContext.Connection.RemoteIpAddress!)
                };
                var result = _userService.CheckSession(userRequest).Result;

                if (!result)
                {
                    HttpContext.Session.Clear();
                    return new JsonResult("true");
                }
                else
                {
                    return new JsonResult("false");
                }
            }
            catch
            {
                HttpContext.Session.Clear();
                return new JsonResult("true");
            }
        }

        public async Task<JsonResult> OnGetLogout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            await _userService.Logout(Convert.ToString(HttpContext.Session.GetString("UserId")!));
            HttpContext.Session.Clear();
            return new JsonResult("logout");
        }

        public async Task<JsonResult> OnGetRefreshSession()
        {
            try
            {
                var userId = HttpContext.Session.GetString("Username");
                if (!string.IsNullOrEmpty(userId))
                {
                    HttpContext.Session.SetString("LastActivity", DateTime.Now.ToString());
                    return new JsonResult("refreshed");
                }
                else
                {
                    return new JsonResult("no-session");
                }
            }
            catch
            {
                return new JsonResult("error");
            }
        }
    }
}