using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Microsoft.AspNetCore.Mvc; 
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.Web.Pages.Application
{
    public class ClientModel(IClientService service) : PageModelExtension
    {
        private readonly IClientService _service = service;

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
                var data = await _service.GetAll(false);

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
        public async Task<JsonResult> OnPostSave([FromBody] Request.Client model)
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
                Request.Client model = new()
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
                Request.Client model = new()
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
