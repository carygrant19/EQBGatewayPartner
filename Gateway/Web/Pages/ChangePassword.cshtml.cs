using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Request = Gateway.BLL.DTO.Request;
namespace Portal.Web.Pages
{
    public class ChangePasswordModel : PageModelExtension
    {
        [BindProperty]
        public string Current { get; set; }
        [BindProperty]
        public string New { get; set; }

        [BindProperty]
        public string Confirm { get; set; }

        [BindProperty]
        public string JavascriptToRun { get; set; }

        //Popup messages

        [BindProperty]
        public SweetAlertMessage SweetAlertMessage { get; set; }

        IConfiguration _configuration;
        IUserService _userService;
        readonly AppConfig _appConfig;

        static readonly string projectOrigin = "[WEB][ChangePassword]";

        public ChangePasswordModel(IConfiguration configuration, IUserService userService)
        {
            _configuration = configuration;
            _appConfig = _configuration.GetSection("AppSettings").Get<AppConfig>()!;
            _userService = userService;
        }

        public IActionResult OnGet()
        {
            SweetAlertMessage = new SweetAlertMessage()
            {
                Title = "",
                Message = "",
                MessageType = ""
            };
            var userId = Convert.ToString(HttpContext.Session.GetString("UserId"))!;
            if (userId == string.Empty || userId == null)
            {
                return Redirect(_appConfig.AppUrl!);
            }
            return Page();
            //string page = ValidateAccess();

            //if (page == "")
            //{
            //    return Page();
            //}
            //else
            //    return Redirect(page);

        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                bool process = true;
                if (Current.Trim() == "" || New.Trim() == "" || Confirm.Trim() == "")
                {
                    SweetAlertMessage = new SweetAlertMessage
                    {
                        Title = "Failed",
                        Message = "Fill out the fields",
                        MessageType = "error"
                    };
                    JavascriptToRun = "ShowPopup()";
                    process = false;
                }
                if (process)
                {
                    Request.UserPassword userPassword = new()
                    {
                        Current = Current,
                        New = New,
                        Confirm = Confirm,
                        Username = HttpContext.Session.GetString("Username"),
                    };

                    var result = _userService.ChangePassword(userPassword).Result;

                    if (result.Status == "SUCCESS")
                    {
                        HttpContext.Session.Clear();
                        JavascriptToRun = "PopupAndLogout()";
                    }
                    else
                    {
                        SweetAlertMessage = new SweetAlertMessage
                        {
                            Title = result.Status,
                            Message = result.Message,
                            MessageType = "error"
                        };
                        JavascriptToRun = "ShowPopup()";
                    }
                }
                return Page();
            }
            catch (Exception ex)
            {
                SweetAlertMessage = new SweetAlertMessage
                {
                    Title = "Error",
                    Message = "Internal server error",
                    MessageType = "error"
                };
                JavascriptToRun = "ShowPopup()";

                return Page();
            }



        }
    }
}