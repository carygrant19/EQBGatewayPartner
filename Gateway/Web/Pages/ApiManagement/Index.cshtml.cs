using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Microsoft.AspNetCore.Mvc;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.Web.Pages.ApiManagement
{
    public class IndexModel(IApiEndpointService service, ICategoryService categoryService) : PageModelExtension
    {
        private readonly IApiEndpointService _service = service;
        private readonly ICategoryService _categoryService = categoryService;

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

        public async Task<JsonResult> OnGetCategories()
        {
            try
            {
                var data = await _categoryService.GetAllActiveAsync();
                return new JsonResult(data);
            }
            catch (Exception ex)
            {
                return new JsonResult(
                    new Response.Result
                    {
                        Status = "ERROR",
                        Message = ex.Message
                    }
                );
            }
        }

        public async Task<JsonResult> OnGetAll()
        {
            try
            {
                var data = await _service.GetActiveEndpointsAsync();
                return new JsonResult(data);
            }
            catch (Exception ex)
            {
                return new JsonResult(
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
                var data = await _service.FilterAsync(param);
                return new JsonResult(data);
            }
            catch (Exception ex)
            {
                return new JsonResult(
                    new Response.Result
                    {
                        Status = "ERROR",
                        Message = ex.Message
                    }
                );
            }
        }

        public async Task<JsonResult> OnPostSave([FromBody] Request.ApiEndpoint model)
        {
            model.OpUser = HttpContext.Session.GetString("Username")!;
            model.OpUserId = HttpContext.Session.GetString("UserId");
            model.Terminal = HttpContext.Session.GetString("Terminal");

            try
            {
                if (string.IsNullOrEmpty(model.Id) || model.Id == "0")
                {
                    return new JsonResult(await _service.CreateAsync(model));
                }
                else
                {
                    return new JsonResult(await _service.UpdateAsync(model));
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(
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
                Request.ApiEndpoint model = new()
                {
                    OpUser = HttpContext.Session.GetString("Username")!,
                    OpUserId = HttpContext.Session.GetString("UserId"),
                    Terminal = HttpContext.Session.GetString("Terminal"),
                    Id = id
                };

                return new JsonResult(await _service.DeleteAsync(model));
            }
            catch (Exception ex)
            {
                return new JsonResult(
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
                Request.ApiEndpoint model = new()
                {
                    OpUser = HttpContext.Session.GetString("Username")!,
                    OpUserId = HttpContext.Session.GetString("UserId"),
                    Terminal = HttpContext.Session.GetString("Terminal"),
                    Id = id
                };

                return new JsonResult(await _service.RestoreAsync(model));
            }
            catch (Exception ex)
            {
                return new JsonResult(
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