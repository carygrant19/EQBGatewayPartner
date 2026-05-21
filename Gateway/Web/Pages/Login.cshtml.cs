using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages; 
using System.Security.Claims;
using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;
namespace Gateway.Web.Pages
{
    public class LoginModel(IConfiguration configuration, IRoleService roleService, IUserService userService, ILDAPService ldapService) : PageModel
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IRoleService _roleService = roleService;
        private readonly IUserService _userService = userService;
        private readonly ILDAPService _ldapService = ldapService;
        readonly AppConfig _appConfig = configuration.GetSection("AppSettings").Get<AppConfig>()!;

        [BindProperty]
        public bool IsLDAP { get; set; } = true;

        [BindProperty]
        public string? Username { get; set; }
        [BindProperty]
        public string? Password { get; set; }
        [BindProperty]
        public string? Message { get; set; }

        [BindProperty]
        public bool IsLogout { get; set; }

        public async Task<IActionResult> OnGet()
        {
            try
            {

                try
                {

                    if (!String.IsNullOrEmpty(await Task.FromResult(HttpContext.Session.GetString("Username"))))
                    {
                        return Redirect(_appConfig.AppUrl + "/Home");
                    }
                    else
                    {
                        return Page();
                    }
                }
                catch
                {
                    return Page();
                }

            }
            catch
            {
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                Message = "";

                // Step 1: Check LDAP connectivity if required
                //if (IsLDAP)
                //{
                //    var ldapConResult = await _ldapService.CheckLDAPConnnectivity();
                //    if (ldapConResult.Status != "SUCCESS")
                //    {
                //        Message = ldapConResult.Message!;
                //        return Page();
                //    }
                //}

                // Step 2: Initialize user and credentials
                var userCredentials = new Request.User { Username = Username!, Password = Password!, Terminal = Convert.ToString(HttpContext.Connection.RemoteIpAddress!) };
                Response.User userInfo;

                // Step 3: Authenticate based on LDAP or UserService
                var user = _userService.ByUsername(Username!).Result;
                if (user != null && user.Username == string.Empty)
                {
                    Message = "User not yet enrolled to Portal";
                    return Page();
                }
                bool IsLDAP = (bool)user.LDAPAuthentication!;

                if (IsLDAP)
                {
                    //userInfo = await _ldapService.LDAPUserDetails(Username!);
                    //if (string.IsNullOrEmpty(userInfo.Username))
                    //{
                    //    Message = "User not exists in Active Directory. Please seek assistance from IT support team.";
                    //    return Page();
                    //} 
                    userInfo = await _ldapService.Login(userCredentials);
                    //if (string.IsNullOrEmpty(userInfo.Username))
                    //{
                    //    Message = "Invalid Username or Password.";
                    //    return Page();
                    //}
                    if (userInfo.Result.Status == "NOTENROLLED")
                    {
                        Message = userInfo.Result.Message;
                        return Page();
                    }
                    if (userInfo.Result.Status != "SUCCESS")
                    {
                        Message = userInfo.Result.Message;
                        return Page();
                    }

                    if ((bool)userInfo.Deleted)
                    {
                        Message = "User has been disabled in Portal.";
                        return Page();
                    }

                }
                else
                {
                    var userValidationResult = await _userService.Validate(userCredentials);
                    if (userValidationResult.Status != "SUCCESS" && userValidationResult.Status != "EXPRPASS")
                    {

                        Message = userValidationResult.Message;
                        return Page();
                    }

                    userInfo = await _userService.ByUsername(Username!);
                    if (userInfo == null)
                    {
                        Message = "User does not exist in the system.";
                        return Page();
                    }
                }

                var claims = new List<Claim>
                        {
                            new(ClaimTypes.Name, Username!)
                        };

                var identity = new ClaimsIdentity(claims, "CookieAuth");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("CookieAuth", principal);

                // Step 4: Manage session variables

                HttpContext.Session.SetString("AppUrl", _appConfig.AppUrl!); 
                HttpContext.Session.SetObject("HeadOffice", _appConfig.HeadOffice!);
                HttpContext.Session.SetObject("LDAPAuthentication", IsLDAP); //should base on checkbox in login, not in database value
                HttpContext.Session.SetString("Token", _appConfig.Token!);
                HttpContext.Session.SetString("UserId", userInfo.Id!);
                HttpContext.Session.SetString("Username", userInfo.Username!);
                HttpContext.Session.SetString("Email", userInfo.Email!);
                HttpContext.Session.SetObject("PasswordExpirationDate", userInfo.PasswordExpirationDate!);
                //HttpContext.Session.SetString("Branch", userInfo.Branch!);
                HttpContext.Session.SetString("BranchCode", userInfo.Branch != null ? userInfo.Branch.Code : "");
                HttpContext.Session.SetString("BranchDesc", userInfo.Branch != null ? userInfo.Branch.Description : "");
                HttpContext.Session.SetString("FullName", $"{userInfo.LastName}, {userInfo.FirstName} {userInfo.MiddleName}");
                //HttpContext.Session.SetString("Phone", userInfo.Phone);
                //HttpContext.Session.SetString("Mobile", userInfo.Mobile);
                HttpContext.Session.SetString("Terminal", HttpContext.Connection.RemoteIpAddress!.ToString());
                HttpContext.Session.SetString("AppName", _configuration["AppSettings:AppName"]!);

                // Step 5: Password Expiry and Active Session Check
                bool passwordExpired = IsLDAP ? await _ldapService.CheckPasswordExpired(Username!, userInfo.LDAPPath) : userInfo.PasswordExpirationDate < DateTime.Now || (bool)userInfo.DefaultPassword!;


                HttpContext.Session.SetString("ChangePassword", passwordExpired ? "1" : "0");

                bool activeSession = await _userService.CheckActiveSession(userCredentials);
                HttpContext.Session.SetString("ActiveSession", activeSession ? "1" : "0");

                // Step 6: Redirect based on session status
                if (!activeSession)
                {
                    if (passwordExpired)
                    {
                        Message = IsLDAP
                            ? "Password is expired. Please seek assistance from IT support team."
                            : null;

                        return IsLDAP ? Page() : Redirect($"{_appConfig.AppUrl}/ChangePassword");
                    }
                    else
                    {
                        var modules = await _roleService.GetRoleModules(Username!.Trim());
                        HttpContext.Session.SetObject("Modules", modules);
                        //log active user
                        //if (!IsLDAP)
                        //{
                        //    await _userService.LogActiveUser(userCredentials);
                        //}
                        return Redirect($"{_appConfig.AppUrl}/Home");



                    }
                }

                return Redirect($"{_appConfig.AppUrl}/ActiveSession");
            }
            catch (Exception ex)
            {
                Message = "Internal Server Error. Please seek assistance from IT support team";
                return Page();
            }

        }
    }
}
