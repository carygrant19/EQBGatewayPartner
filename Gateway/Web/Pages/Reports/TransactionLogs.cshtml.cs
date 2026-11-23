using Gateway.BLL.Helper;
using Gateway.BLL.Services.IServices;
using Microsoft.AspNetCore.Mvc;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.Web.Pages.Reports
{
    public class TransactionLogsModel(IReportService service, IDashboardService dashboardService) : PageModelExtension
    {
        private readonly IReportService _service = service;
        private readonly IDashboardService _dashboardService = dashboardService;

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

        public async Task<JsonResult> OnGetRoutes()
        {
            try
            {
                var data = await _service.GetRoutes();

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
            param.OpUser = HttpContext.Session.GetString("Username")!;
            param.OpUserId = HttpContext!.Session.GetString("UserId");
            param.Terminal = HttpContext.Session.GetString("Terminal");

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

        public async Task<IActionResult> OnPostExportCsv([FromBody] Request.FParam param)
        {
            param.OpUser = HttpContext.Session.GetString("Username")!;
            param.OpUserId = HttpContext!.Session.GetString("UserId");
            param.Terminal = HttpContext.Session.GetString("Terminal");

            try
            {
                var csvBytes = await _service.ExportCsv(param);
                string fileName = $"Gateway_Transaction_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                return File(csvBytes, "text/csv", fileName);
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

        public async Task<IActionResult> OnGetTraceDetails(string traceId, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(traceId))
                {
                    return new JsonResult(
                        new Response.Result
                        {
                            Status = "ERROR",
                            Message = "TraceId is required."
                        }
                    );
                }

                var trace = await _dashboardService.GetTraceTimelineAsync(traceId, cancellationToken);
                if (trace == null)
                {
                    return new JsonResult(
                        new Response.Result
                        {
                            Status = "ERROR",
                            Message = "Trace timeline not found."
                        }
                    );
                }

                return new JsonResult(trace);
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
        public async Task<JsonResult> OnGetClients()
        {
            try
            {
                var data = await _service.GetClients();

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
    }
}