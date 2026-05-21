using Gateway.BLL.Services.IService; 
using Microsoft.AspNetCore.Http;
using System.Text;
using Model = Gateway.Data.Models;
using Response = Gateway.BLL.DTO.Response;
namespace Gateway.BLL.Helper
{
    public class Common 
    {
        private readonly IApiClientService _apiClientService;

        public Common(IApiClientService apiClientService)
        {
            _apiClientService = apiClientService;
        }

        public async Task<Response.ApiClient> GetApiClient(string apiKey)
        {
            return await _apiClientService.ByApiKey(apiKey!);
        }

        public static bool IsWithinTimeLimit(TimeSpan? timeFrom, TimeSpan? timeTo)
        {
            if (timeFrom.HasValue && timeTo.HasValue)
            {
                TimeSpan now = DateTime.Now.TimeOfDay;
                return now > timeFrom.Value && now < timeTo.Value;
            }
            return true;
        }

        public static bool ValidateRequestMethodAndSignature(HttpContext context, Response.ApiClient client, out string message)
        {
            if (context.Request.Method.Equals("post", StringComparison.OrdinalIgnoreCase))
            {
                if (ValidateRequest.Signature(context, client).Equals("valid", StringComparison.OrdinalIgnoreCase))
                {
                    message = null;
                    return true;
                }
                message = "Invalid Signature";
                return false;
            }
            message = null;
            return true;
        }

        public static bool ValidateAccess(List<Response.RoleModule> modules, string path)
        {
            var result = false;

            foreach (var item in modules)
            {
                //mon
                if (item.Url.ToUpper().Equals(path.ToUpper()))
                {
                    result = true;
                    break;
                }
            }
            return result;
        }
        public static Model.ModuleProperty GetModuleProperty(List<Response.RoleModule> modules, string path)
        {

            Model.ModuleProperty prop = new();

            foreach (var item in modules)
            {
                if (item.Url.Equals(path))
                {
                    prop.Url = item.Url;
                    prop.Permission = item.Permissions;
                    prop.Name = item.Name;
                    prop.Icon = item.Icon;
                    break;
                }
            }

            return prop;
        }

        public static string GetActions(List<Response.RoleModule> modules, string path)
        {
            var result = "";

            foreach (var item in modules)
            {
                if (item.Url.Contains(path))
                {
                    result = item.Permissions;
                    break;
                }
            }
            return result;
        }
        public static string Navigation(List<Response.RoleModule> modules, string currentPage)
        {
            StringBuilder sb = new();

            try
            {
                sb.Append(@"<style>
            .collapse.show {
                display: block !important;
            }
        </style>");

                sb.Append("<ul class=\"navbar-nav mb-auto w-100\">");
                sb.Append("<li class=\"menu-label mt-2\"><span>Navigation</span></li>");

                foreach (var item in modules)
                {
                    if (!item.Show || item.ParentId != "0") continue;

                    int childCount = modules.Count(i => i.ParentId == item.Id && i.Show);
                    currentPage += "/";
                    bool isActive = currentPage.Contains(item.Url + "/", StringComparison.OrdinalIgnoreCase) ||
                                    HasActiveChild(modules, currentPage, item.Id);

                    string uniqueId = $"sidebar_{item.Id}";

                    sb.Append("<li class=\"nav-item\">");

                    if (childCount > 0)
                    {
                        sb.AppendFormat(
                            "<a class=\"nav-link\" href=\"#{0}\" data-bs-toggle=\"collapse\" data-bs-target=\"#{0}\">" +
                            "<i class=\"{1} menu-icon\"></i> <span>{2}</span></a>",
                            uniqueId, item.Icon, item.Name
                        );

                        sb.AppendFormat("<div class=\"collapse {0}\" id=\"{1}\">", isActive ? "show" : "", uniqueId);
                        sb.Append("<ul class=\"nav flex-column\">");
                        sb.Append(LoadChildNav(modules, currentPage, item.Id, 1));
                        sb.Append("</ul></div>");
                    }
                    else
                    {
                        sb.AppendFormat("<a class=\"nav-link\" href=\"{0}\"><i class=\"{1} menu-icon\"></i> <span>{2}</span></a>",
                            string.IsNullOrEmpty(item.Url) ? "#" : item.Url,
                            item.Icon,
                            item.Name
                        );
                    }

                    sb.Append("</li>");
                }

                sb.Append("</ul>");

                return sb.ToString();
            }
            catch
            {
                return sb.ToString();
            }
        }

        private static string LoadChildNav(List<Response.RoleModule> modules, string currentPage, string parentId, int level)
        {
            StringBuilder sb = new();

            try
            {
                foreach (var item in modules)
                {
                    if (!item.Show || item.ParentId != parentId) continue;

                    int childCount = modules.Count(i => i.ParentId == item.Id && i.Show);
                    bool isActive = currentPage.Contains(item.Url, StringComparison.OrdinalIgnoreCase) ||
                                    HasActiveChild(modules, currentPage, item.Id);

                    string uniqueId = $"sidebar_{item.Id}";
                    string paddingStyle = $"style=\"padding-left: {level * 5}px\""; // Increased indentation

                    sb.AppendFormat("<li class=\"nav-item\" {0}>", paddingStyle);

                    if (childCount > 0)
                    {
                        sb.AppendFormat(
                            "<a class=\"nav-link\" href=\"#{0}\" data-bs-toggle=\"collapse\" data-bs-target=\"#{0}\">" +
                            "<i class=\"{1} menu-icon\"></i><span>{2}</span></a>",
                            uniqueId, item.Icon, item.Name
                        );

                        sb.AppendFormat("<div class=\"collapse {0}\" id=\"{1}\">", isActive ? "show" : "", uniqueId);
                        sb.Append("<ul class=\"nav flex-column\">");
                        sb.Append(LoadChildNav(modules, currentPage, item.Id, level + 1));
                        sb.Append("</ul></div>");
                    }
                    else
                    {
                        sb.AppendFormat(
                            "<a class=\"nav-link\" href=\"{0}\">" +
                            "<i class=\"{1} menu-icon\"></i><span>{2}</span></a>",
                            string.IsNullOrEmpty(item.Url) ? "" : item.Url,
                            item.Icon,
                            item.Name
                        );
                    }

                    sb.Append("</li>");
                }

                return sb.ToString();
            }
            catch
            {
                return sb.ToString();
            }
        }

        private static bool HasActiveChild(List<Response.RoleModule> modules, string currentPage, string parentId)
        {
            // Recursively check if any child of the parent has the active URL
            foreach (var item in modules)
            {
                if (item.Show && item.ParentId == parentId)
                {
                    if (currentPage.Equals(item.Url, StringComparison.OrdinalIgnoreCase) || HasActiveChild(modules, currentPage, item.Id))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
