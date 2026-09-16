using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Gateway.Data.Models;
using System.Text;
using Response = Gateway.BLL.DTO.Response;
using Microsoft.AspNetCore.Http;

namespace Gateway.BLL.Helper
{
    [Authorize]
    public class PageModelExtension : PageModel
    {
        [BindProperty]
        public string BulkUploaderType { get; set; } = string.Empty;
        [BindProperty]
        public string PageTitle { get; set; } = string.Empty;
        [BindProperty]
        public string PageUrl { get; set; } = string.Empty;
        [BindProperty]
        public string Permission { get; set; } = string.Empty;
        [BindProperty]
        public string Icon { get; set; } = string.Empty;

        public string ValidateAccess()
        {
            string domain = $"{Request.Scheme}://{Request.Host.Value}";
            try
            {
                if (!String.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
                {

                    if (HttpContext.Session.GetString("ChangePassword") == "0")
                    {

                        var modules = HttpContext.Session.GetObject<List<Response.RoleModule>>("Modules");
                        var hasAccess = false;
                        var currentPage = HttpContext.Request.Path.Value;

                        if (modules != null)
                        {
                            ViewData["Navigation"] = Common.Navigation(modules!, currentPage!);
                            hasAccess = Common.ValidateAccess(modules, HttpContext.Request.Path);
                        }
                        else
                            ViewData["Navigation"] = null;

                        if (hasAccess)
                        {

                            var pageProperty = Common.GetModuleProperty(modules!, HttpContext.Request.Path);

                            Permission = pageProperty.Permission;
                            PageTitle = pageProperty.Name;
                            Icon = pageProperty.Icon;
                            PageUrl = pageProperty.Url;

                            Permission = (Common.GetActions(modules!, HttpContext.Request.Path));
                            return "";
                        }
                        else
                            return $"{domain}/Error?code=403";
                    }
                    else
                        return $"{domain}/Error?code=801";

                }
                else
                {
                    return domain;
                }
            }
            catch
            {
                return domain;
            }
        }

    } 
}
