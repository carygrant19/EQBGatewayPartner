using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.Web.Pages.Maintenance
{
    public class OutboundAuthProfileModel(IOutboundAuthProfileService service) : PageModelExtension
    {
        private readonly IOutboundAuthProfileService _service = service;

        public IActionResult OnGet()
        {
            string page = ValidateAccess();

            if (string.IsNullOrEmpty(page))
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
                var data = await _service.Get("");
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

        public async Task<JsonResult> OnPostSave([FromBody] Request.OutboundAuthProfile model)
        {
            try
            {
                PopulateAuditFields(model);

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

        public async Task<JsonResult> OnPutDelete([FromBody] Request.OutboundAuthProfile model)
        {
            try
            {
                PopulateAuditFields(model);
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

        public async Task<JsonResult> OnPutRestore([FromBody] Request.OutboundAuthProfile model)
        {
            try
            {
                PopulateAuditFields(model);
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

        private void PopulateAuditFields(Request.OutboundAuthProfile model)
        {
            model.OpUser = HttpContext.Session.GetString("Username") ?? "SYSTEM";
            model.OpUserId = HttpContext.Session.GetString("UserId") ?? "0";
            model.Terminal = HttpContext.Session.GetString("Terminal") ?? "LOCAL";
        }
    }
}