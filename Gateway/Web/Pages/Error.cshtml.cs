using Gateway.BLL.Helper;
using Gateway.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages; 
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.Web.Pages
{
    public class ErrorModel(IConfiguration configuration) : PageModel
    {
        [BindProperty]
        public string? Actions { get; set; }
        [BindProperty]
        public string? PageTitle { get; set; }
        [BindProperty]
        public string? Icon { get; set; }
        [BindProperty]
        public string? PageUrl { get; set; }

        [BindProperty]
        public string? Code { get; set; }
        [BindProperty]
        public string? Title { get; set; }
        [BindProperty]
        public string? Message { get; set; }

        //readonly IConfiguration _configuration = configuration;

        readonly AppConfig _appConfig = configuration.GetSection("AppSettings").Get<AppConfig>()!;

        public IActionResult OnGet()
        {
            try
            {
                if (!String.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
                {
                    var modules = HttpContext.Session.GetObject<List<Response.RoleModule>>("Modules");
                    var currentPage = HttpContext.Request.Path.Value;

                    ViewData["Navigation"] = Common.Navigation(modules!, currentPage!);

                    var error = ErrorMessage(HttpContext.Request.Query["code"]!);

                    Code = error.Code!;
                    Title = error.Title!;
                    Message = error.Message!;

                    return Page();

                }
                else
                {
                    return Redirect(_appConfig.AppUrl!);
                }
            }
            catch
            {
                return Redirect(_appConfig.AppUrl!);
            }
        }

        public IActionResult OnPostReturnToLogin()
        {
            HttpContext.Session.Clear(); // Clear the session

            // Redirect using a direct string path instead of dynamic page-route calculation
            return Redirect("~/");
        }
        public static Error ErrorMessage(string code)
        {
            var error = new Error
            {
                Code = code
            };

            switch (code)
            {

                case "400":
                    error.Title = "Bad Request";
                    error.Message = "Bad Request";
                    break;

                case "401":
                case "403":
                    error.Title = "Unauthorized";
                    error.Message = "Access is denied due to invalid credentials.";
                    break;

                case "404":
                    error.Title = "Not Found";
                    error.Message = "We could not find the page you were looking for.";
                    break;

                case "800":
                    error.Title = "Not Found";
                    error.Message = "Bad Request";
                    break;

                case "801":
                    error.Title = "Change Password Required";
                    error.Message = "You are using a default or expired password.";
                    break;

                default:
                    error.Code = "500";
                    error.Title = "Internal Server Error";
                    error.Message = "We will work on fixing that right away.";
                    break;

            }

            return error;
        }
    }

}
