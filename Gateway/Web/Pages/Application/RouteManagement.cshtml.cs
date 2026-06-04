using Gateway.BLL.Helper;
using Gateway.BLL.Services;
using Gateway.BLL.Services.IService;
using Gateway.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;
namespace Gateway.Web.Pages.Application
{
    public class RouteManagementModel(IRouteService service, IConfiguration configuration) : PageModelExtension
    {
        private readonly IRouteService _service = service; 
        private readonly OcelotConfigFile _ocelotConfigFile = configuration.GetSection("OcelotConfigFile").Get<OcelotConfigFile>()!;
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
        public async Task<JsonResult> OnPostSave([FromBody] Request.Route model)
        {
            model.OpUser = HttpContext.Session.GetString("Username")!;
            model.OpUserId = HttpContext!.Session.GetString("UserId");
            model.Terminal = HttpContext.Session.GetString("Terminal");

            try
            {
                if (Convert.ToString(model.Id) == "0" || Convert.ToString(model.Id) == string.Empty)
                {
                    return new JsonResult(await _service.Create(model));
                }
                return new JsonResult(await _service.Update(model));
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

        public async Task<JsonResult> OnGetPublishOcelot()
        {
            var result = new Response.Result();
            //var ocelotConfig = _routeService.Config().Result; 
            result = await _service.GenerateOcelotConfigFile(_ocelotConfigFile);

            return new JsonResult(result);
        }
    }
}
