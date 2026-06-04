using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Microsoft.AspNetCore.Mvc;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.Pages.Application
{
    public class RouteCategoryModel(IRouteService service) : PageModelExtension
    {

        private readonly IRouteService _service = service;

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

        public async Task<JsonResult> OnGetAllCategory()
        {

            try
            {
                var data = await _service.GetCategory();

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