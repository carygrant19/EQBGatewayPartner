using Microsoft.AspNetCore.Mvc;
using Gateway.BLL.Services.IService;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;
using Gateway.BLL.Helper;

namespace Gateway.Web.Pages.Security
{
    public class ModuleModel(IModuleService service) : PageModelExtension
    {
        private readonly IModuleService _service = service;

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
        public async Task<JsonResult> OnGetModuleGroup()
        {
            try
            {
                var data = await _service.GetModuleGroup();

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
        public async Task<JsonResult> OnGetAllParent()
        {

            try
            {
                var data = await _service.GetAllParent();

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
        public async Task<JsonResult> OnPostSave([FromBody] Request.Module model)
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
                Request.Module model = new()
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
                Request.Module model = new()
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

    }
}
