using Gateway.BLL.Services.IService;
using Microsoft.AspNetCore.Http;
using System.Text;
using Model = Gateway.Data.Models;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Helper
{
    public class Common(IClientService apiClientService)
    {
        private readonly IClientService _apiClientService = apiClientService;

        public async Task<Response.Client> GetApiClient(string apiKey)
        {
            return await _apiClientService.ByApiKey(apiKey);
        }
        public static bool IsEndpointTimeValid(DateTime? dateFrom, DateTime? dateTo, TimeSpan? timeFrom, TimeSpan? timeTo, string? allowedDays)
        {
            var now = DateTime.Now;

            if (dateFrom.HasValue && now.Date < dateFrom.Value.Date) return false;
            if (dateTo.HasValue && now.Date > dateTo.Value.Date) return false;

            if (!string.IsNullOrWhiteSpace(allowedDays))
            {
                string currentDayInt = ((int)now.DayOfWeek).ToString();

                var validDays = allowedDays.Split(',').Select(d => d.Trim()).ToList();

                if (!validDays.Contains(currentDayInt)) return false;
            }

            if (timeFrom.HasValue && timeTo.HasValue)
            {
                var timeNow = now.TimeOfDay;
                if (timeNow < timeFrom.Value || timeNow > timeTo.Value) return false;
            }

            return true;
        }
        public static bool IsWithinTimeLimit(TimeSpan? timeFrom, TimeSpan? timeTo)
        {
            if (timeFrom.HasValue && timeTo.HasValue)
            {
                TimeSpan now = DateTime.Now.TimeOfDay;
                return now >= timeFrom.Value && now <= timeTo.Value;
            }
            return true;
        }

        public static async Task<(bool IsValid, string? Message)> ValidateRequestMethodAndSignatureAsync(HttpContext context, Response.Client client)
        {
            if (HttpMethods.IsPost(context.Request.Method) ||
                HttpMethods.IsPut(context.Request.Method) ||
                HttpMethods.IsPatch(context.Request.Method))
            {
                string result = await ValidateRequest.SignatureAsync(context, client);
                if (string.Equals(result, "Valid", StringComparison.OrdinalIgnoreCase))
                {
                    return (true, null);
                }
                return (false, "Invalid Signature");
            }

            return (true, null);
        }

        public static bool ValidateAccess(List<Response.RoleModule> modules, string path)
        {
            if (modules == null || string.IsNullOrEmpty(path)) return false;

            return modules.Any(item => string.Equals(item.Url, path, StringComparison.OrdinalIgnoreCase));
        }

        public static Model.ModuleProperty GetModuleProperty(List<Response.RoleModule> modules, string path)
        {
            Model.ModuleProperty prop = new();

            if (modules == null || string.IsNullOrEmpty(path)) return prop;

            var item = modules.FirstOrDefault(i => string.Equals(i.Url, path, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                prop.Url = item.Url;
                prop.Permission = item.Permissions;
                prop.Name = item.Name;
                prop.Icon = item.Icon;
            }

            return prop;
        }

        public static string GetActions(List<Response.RoleModule> modules, string path)
        {
            if (modules == null || string.IsNullOrEmpty(path)) return string.Empty;

            var item = modules.FirstOrDefault(i => i.Url.Contains(path, StringComparison.OrdinalIgnoreCase));
            return item?.Permissions ?? string.Empty;
        }

        public static string Navigation(List<Response.RoleModule> modules, string currentPage)
        {
            if (modules == null || !modules.Any()) return string.Empty;

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
                    string currentPathWithSlash = currentPage + "/";
                    bool isActive = currentPathWithSlash.Contains(item.Url + "/", StringComparison.OrdinalIgnoreCase) ||
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
                    string paddingStyle = $"style=\"padding-left: {level * 5}px\"";

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