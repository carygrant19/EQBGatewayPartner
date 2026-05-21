using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Gateway.BLL.Helper; // Critical: Imports SetObject extensions
using Gateway.BLL.Services.IService;
using Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IRoleService _roleService;
        private readonly IUserService _userService;
        private readonly ILDAPService _ldapService;
        private readonly AppConfig _appConfig;

        public AuthController(
            IConfiguration configuration,
            IRoleService roleService,
            IUserService userService,
            ILDAPService ldapService)
        {
            _configuration = configuration;
            _roleService = roleService;
            _userService = userService;
            _ldapService = ldapService;
            _appConfig = configuration.GetSection("AppSettings").Get<AppConfig>()!;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
                {
                    return BadRequest(new { message = "Username and password are required." });
                }

                // Step 1: Initialize user and credentials
                var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var userCredentials = new Request.User
                {
                    Username = model.Username,
                    Password = model.Password,
                    Terminal = remoteIp
                };

                Response.User userInfo;

                // Step 2: Authenticate based on LDAP or UserService database identity profiles
                var user = _userService.ByUsername(model.Username).Result;
                if (user != null && user.Username == string.Empty)
                {
                    return BadRequest(new { message = "User not yet enrolled to Portal" });
                }

                bool isLdap = (bool)user!.LDAPAuthentication!;

                // Step 3: Check credentials using LDAP or Native verification pipelines
                if (isLdap)
                {
                    userInfo = await _ldapService.Login(userCredentials);

                    if (userInfo.Result.Status == "NOTENROLLED")
                    {
                        return BadRequest(new { message = userInfo.Result.Message });
                    }
                    if (userInfo.Result.Status != "SUCCESS")
                    {
                        return BadRequest(new { message = userInfo.Result.Message });
                    }
                    if ((bool)userInfo.Deleted!)
                    {
                        return BadRequest(new { message = "User has been disabled in Portal." });
                    }
                }
                else
                {
                    var userValidationResult = await _userService.Validate(userCredentials);
                    if (userValidationResult.Status != "SUCCESS" && userValidationResult.Status != "EXPRPASS")
                    {
                        return BadRequest(new { message = userValidationResult.Message });
                    }

                    userInfo = await _userService.ByUsername(model.Username)!;
                    if (userInfo == null)
                    {
                        return BadRequest(new { message = "User does not exist in the system." });
                    }
                }

                // Step 4: Issue Cookie Context Tracker
                var claims = new List<Claim> { new(ClaimTypes.Name, model.Username) };
                var identity = new ClaimsIdentity(claims, "CookieAuth");
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync("CookieAuth", principal);

                // Step 5: Manage state session mapping variables via the imported BLL Helpers
                HttpContext.Session.SetString("AppUrl", _appConfig.AppUrl!);
                HttpContext.Session.SetObject("HeadOffice", _appConfig.HeadOffice!);
                HttpContext.Session.SetObject("LDAPAuthentication", isLdap);
                HttpContext.Session.SetString("Token", _appConfig.Token!);
                HttpContext.Session.SetString("UserId", userInfo.Id!);
                HttpContext.Session.SetString("Username", userInfo.Username!);
                HttpContext.Session.SetString("Email", userInfo.Email!);
                HttpContext.Session.SetObject("PasswordExpirationDate", userInfo.PasswordExpirationDate!);
                HttpContext.Session.SetString("BranchCode", userInfo.Branch != null ? userInfo.Branch.Code : "");
                HttpContext.Session.SetString("BranchDesc", userInfo.Branch != null ? userInfo.Branch.Description : "");
                HttpContext.Session.SetString("FullName", $"{userInfo.LastName}, {userInfo.FirstName} {userInfo.MiddleName}");
                HttpContext.Session.SetString("Terminal", remoteIp);
                HttpContext.Session.SetString("AppName", _configuration["AppSettings:AppName"]!);

                // Step 6: Life-Cycle Validations (Password Expiration and Dual-Session Checks)
                bool passwordExpired = isLdap
                    ? await _ldapService.CheckPasswordExpired(model.Username, userInfo.LDAPPath)
                    : userInfo.PasswordExpirationDate < DateTime.Now || (bool)userInfo.DefaultPassword!;

                HttpContext.Session.SetString("ChangePassword", passwordExpired ? "1" : "0");

                bool activeSession = await _userService.CheckActiveSession(userCredentials);
                HttpContext.Session.SetString("ActiveSession", activeSession ? "1" : "0");

                // Step 7: Send structured navigation payloads back to your React app view router
                if (activeSession)
                {
                    return Ok(new { status = "ACTIVESESSION", redirectUrl = "/ActiveSession" });
                }

                if (passwordExpired)
                {
                    if (isLdap)
                    {
                        return BadRequest(new { message = "Password is expired. Please seek assistance from IT support team." });
                    }
                    return Ok(new { status = "PASSWORD_EXPIRED", redirectUrl = "/ChangePassword" });
                }

                // Everything is clean: Cache user module navigation nodes
                var modules = await _roleService.GetRoleModules(model.Username.Trim());
                HttpContext.Session.SetObject("Modules", modules);

                return Ok(new { status = "SUCCESS", redirectUrl = "/Home" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Internal Server Error. Please seek assistance from IT support team" });
            }
        }

        [HttpGet("status")]
        public IActionResult GetAuthStatus()
        {
            var sessionUser = HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(sessionUser))
            {
                return Ok(new { isAuthenticated = true, username = sessionUser });
            }
            return Ok(new { isAuthenticated = false });
        }
    }

    public class LoginRequestModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}