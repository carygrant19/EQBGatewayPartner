using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService; 
using Microsoft.AspNetCore.Mvc; 
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.Web.Pages.Security
{
    public class RoleModel(IConfiguration configuration, IRoleService roleService) : PageModelExtension
    { 
        readonly IRoleService _service = roleService; 

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
        public async Task<JsonResult> OnGetModuleByRoleId(string roleId)
        {
            try
            {
                var result = await _service.ById(roleId);

                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                return new JsonResult(ex);
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

        public async Task<JsonResult> OnPostSave([FromBody] Request.Role model)
        {
            model.OpUser = HttpContext.Session.GetString("Username")!;
            model.OpUserId = HttpContext!.Session.GetString("UserId");
            model.Terminal = HttpContext.Session.GetString("Terminal");

            var result = new Response.Result();

            if (model.Id.Trim() != "")
                result = await _service.Update(model);
            else
                result = await _service.Create(model);

            return new JsonResult(result);
        }
        public async Task<JsonResult> OnPutDelete(string id)
        {
            try
            {
                Request.Role model = new()
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
                Request.Role model = new()
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