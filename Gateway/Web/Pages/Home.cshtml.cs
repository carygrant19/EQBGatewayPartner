using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Model = Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

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

        public async Task<JsonResult> OnPostFilterActivityLog([FromBody] Request.FParam param)
        {
            try
            {
                param.Filters.Add(
                    new() { Property = "UserId", Value = Convert.ToInt32(HttpContext.Session.GetString("UserId")), Operator = "EQUALS" }
                );
                var data = await _userService.FilterActivityLog(param);
                return new JsonResult(data);
            }
            catch (Exception ex)
            {
                return new(
                    new Response.Result
                    {
                        Status = "ERROR",
                        Message = ex.Message
                    }
                );
            }
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

        public async Task<IActionResult> OnGetUserProfileImage()
        {
            try
            {
                var username = Convert.ToString(HttpContext.Session.GetString("Username"));
                var user = await _userService.ByUsername(username!);

                if (user?.ImageContent != null && user.ImageType != null)
                    return File(user.ImageContent, user.ImageType);
                else
                    return File("~/images/default-user.png", "image/png");
            }
            catch
            {
                return File("~/images/default-user.png", "image/png");
            }
        }

        public async Task<IActionResult> OnGetUserProfileImageThumbnail()
        {
            try
            {
                var username = Convert.ToString(HttpContext.Session.GetString("Username"));
                var user = await _userService.ByUsername(username!);

                if (user?.ImageContentThumbnail != null && user.ImageType != null)
                    return File(user.ImageContentThumbnail, user.ImageType);
                else
                    return File("~/images/default-user.png", "image/png");
            }
            catch
            {
                return File("~/images/default-user.png", "image/png");
            }
        }

        public async Task<JsonResult> OnPostChangeProfileImage(List<IFormFile> images)
        {
            try
            {
                var user = new Request.User();
                if (images.Count > 0)
                {
                    using MemoryStream imageMS = new();
                    images[0].CopyTo(imageMS);

                    user.Id = Convert.ToString(HttpContext.Session.GetString("UserId"))!;
                    user.Username = HttpContext.Session.GetString("Username")!;
                    user.OpUser = HttpContext.Session.GetString("Username");
                    user.OpUserId = Convert.ToString(HttpContext.Session.GetString("UserId"));
                    user.ImageContent = imageMS.ToArray();
                    user.ImageType = images[0].ContentType;
                }
                var result = await _userService.ChangeProfileImage(user);
                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                return new JsonResult(ex.ToString());
            }
        }

        public JsonResult OnGetSessionExpired()
        {
            try
            {
                var userRequest = new Request.User()
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