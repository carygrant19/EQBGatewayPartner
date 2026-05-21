using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Microsoft.AspNetCore.Mvc;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.Web.Pages.Gateway
{
    public class RouteModel(IRouteService service) : PageModelExtension
    {
        private readonly IRouteService _service = service;
        private readonly string _moduleName = "Route";

        public IActionResult OnGet()
        { 
            string page = ValidateAccess();

            if (page == "")
            {
                return Page();
            }
            else
            {
                return Redirect(page);
            }
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
                return new JsonResult(new Response.Result
                {
                    Status = "ERROR",
                    Message = ex.Message
                });
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
                return new JsonResult(new Response.Result
                {
                    Status = "ERROR",
                    Message = ex.Message
                });
            }
        }

        public async Task<JsonResult> OnPostSave([FromBody] Request.Route model)
        { 
            model.OpUser = HttpContext.Session.GetString("Username")!;
            model.OpUserId = HttpContext.Session.GetString("UserId");
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
                return new JsonResult(new Response.Result
                {
                    Status = "ERROR",
                    Message = ex.Message
                });
            }
        }

        public async Task<JsonResult> OnPutDelete(string id)
        {
            try
            {
                Request.Route model = new()
                {
                    OpUser = HttpContext.Session.GetString("Username")!,
                    OpUserId = HttpContext.Session.GetString("UserId"),
                    Terminal = HttpContext.Session.GetString("Terminal"),
                    Id = id
                };

                return new JsonResult(await _service.Delete(model));
            }
            catch (Exception ex)
            {
                return new JsonResult(new Response.Result
                {
                    Status = "ERROR",
                    Message = ex.Message
                });
            }
        }

        public async Task<JsonResult> OnPutRestore(string id)
        {
            try
            {
                Request.Route model = new()
                {
                    OpUser = HttpContext.Session.GetString("Username")!,
                    OpUserId = HttpContext.Session.GetString("UserId"),
                    Terminal = HttpContext.Session.GetString("Terminal"),
                    Id = id
                };

                return new JsonResult(await _service.Restore(model));
            }
            catch (Exception ex)
            {
                return new JsonResult(new Response.Result
                {
                    Status = "ERROR",
                    Message = ex.Message
                });
            }
        }
    }
}