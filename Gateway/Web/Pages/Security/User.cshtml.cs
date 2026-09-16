using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Microsoft.AspNetCore.Mvc; 
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.Web.Pages.Security
{
    public class UserModel(IUserService service, ILDAPService ldapService, IWebHostEnvironment webHostEnvironment) : PageModelExtension
    {
        private readonly IUserService _service = service;
        private readonly ILDAPService _ldapService = ldapService;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
        public IActionResult OnGet()
        {
            string page = ValidateAccess();

            if (page == "")
            {
                return Page();
            }
            else
                return Redirect(page);

        }

        public async Task<JsonResult> OnGetAll()
        {

            try
            {
                var data = await _service.Get();

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
        public async Task<JsonResult> OnPostFilter([FromBody] Request.FParam param)
        {
            try
            {
                var data = await _service.Filter(param);
                string imagePath = "images/default-user.png";
                string webRootPath = _webHostEnvironment.WebRootPath;
                string imageFilePath = Path.Combine(webRootPath, imagePath);
                if (System.IO.File.Exists(imageFilePath))
                {
                    try
                    {
                        byte[] imageBytes = System.IO.File.ReadAllBytes(imageFilePath);

                        foreach (var item in data.Data)
                        {
                            item.ImageContent ??= imageBytes;
                        };
                    }
                    catch //(Exception ex)
                    {

                    }
                }

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
        public async Task<JsonResult> OnPostSave([FromBody] Request.User model)
        {
            model.OpUser = HttpContext.Session.GetString("Username")!;
            model.OpUserId = HttpContext!.Session.GetString("UserId");
            model.Terminal = HttpContext.Session.GetString("Terminal");

            try
            {
                if (string.IsNullOrEmpty(model.Id))
                {
                    return new JsonResult(await _service.Create(model));
                }
                else
                {
                    return new JsonResult(await _service.Update(model));
                }
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

        public async Task<JsonResult> OnPutDelete(string id)
        {
            try
            {
                Request.User model = new()
                {
                    OpUser = HttpContext.Session.GetString("Username")!,
                    OpUserId = HttpContext!.Session.GetString("UserId"),
                    Terminal = HttpContext.Session.GetString("Terminal"),
                    Id = id
                };

                return new JsonResult(await _service.Delete(model));

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

        public async Task<JsonResult> OnPutRestore(string id)
        {
            try
            {
                Request.User model = new()
                {
                    OpUser = HttpContext.Session.GetString("Username")!,
                    OpUserId = HttpContext!.Session.GetString("UserId"),
                    Terminal = HttpContext.Session.GetString("Terminal"),
                    Id = id
                };

                return new JsonResult(await _service.Restore(model));

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
        public async Task<JsonResult> OnPostCheckAD([FromBody] string username)
        {
            var model = new Request.User();
            model.Username = username;
            model.OpUser = HttpContext.Session.GetString("Username")!;
            model.OpUserId = HttpContext!.Session.GetString("UserId");
            model.Terminal = HttpContext.Session.GetString("Terminal");

            var result = await _ldapService.LDAPUserDetails(model.Username);

            return new JsonResult(result);
        }
        public async Task<JsonResult> OnPostResetPassword([FromBody] string username)
        {
            var model = new Request.User();
            model.Username = username;
            model.OpUser = HttpContext.Session.GetString("Username")!;
            model.OpUserId = HttpContext!.Session.GetString("UserId");
            model.Terminal = HttpContext.Session.GetString("Terminal");

            var result = new Response.Result();
            result = await _service.ResetPassword(model);

            return new JsonResult(result);
        }
        public async Task<JsonResult> OnPostUnlockUser([FromBody] string username)
        {
            var model = new Request.User();
            model.Username = username;
            model.OpUser = HttpContext.Session.GetString("Username")!;
            model.OpUserId = HttpContext!.Session.GetString("UserId");
            model.Terminal = HttpContext.Session.GetString("Terminal");

            var result = new Response.Result();
            result = await _service.UnlockUser(model);

            return new JsonResult(result);
        }
    }
}
