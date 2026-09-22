using Gateway.BLL.Helper;
using Gateway.BLL.Services;
using Gateway.BLL.Services.IService;
using Gateway.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Yarp.ReverseProxy.Model;
using Model = Gateway.Data.Models;

namespace Gateway.Proxy.Middleware
{
    public class SecurityMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(
            HttpContext context,
            IClientService apiClientService,
            ICertificateValidatorService certValidator,
            IAuthService authService,
            EFDbContext dbContext)
        {
            string traceId = context.TraceIdentifier;
            var path = context.Request.Path.Value ?? "";

            // PHASE 1: HEALTH CHECK EXEMPTION
            if (path.Equals("/health", StringComparison.OrdinalIgnoreCase) || path.EndsWith("/health", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            // PHASE 2: ROUTE MATCHING (YARP Metadata or DB Fallback)
            var routeModel = context.GetEndpoint()?.Metadata.GetMetadata<RouteModel>();
            string? routeCode = routeModel?.Config.RouteId;

            Model.Route? endpoint = null;

            if (!string.IsNullOrEmpty(routeCode))
            {
                endpoint = await dbContext.Set<Model.Route>()
                    .Include(e => e.IpRules)
                    .Include(e => e.AuthProvider)
                    .Include(e => e.Transforms)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Code == routeCode && e.IsActive);
            }

            if (endpoint == null)
            {
                var activeEndpoints = await dbContext.Set<Model.Route>()
                    .Include(e => e.IpRules)
                    .Include(e => e.AuthProvider)
                    .Include(e => e.Transforms)
                    .Where(e => e.IsActive)
                    .AsNoTracking()
                    .ToListAsync();

                endpoint = activeEndpoints.FirstOrDefault(e =>
                {
                    bool hasCatchAll = e.UpstreamPathTemplate.Contains("{**catch-all}", StringComparison.OrdinalIgnoreCase)
                                    || e.UpstreamPathTemplate.Contains("{**remainder}", StringComparison.OrdinalIgnoreCase);

                    var cleanPath = RouteTemplateHelper.StripWildcards(e.UpstreamPathTemplate).TrimEnd('/');

                    if (string.IsNullOrEmpty(cleanPath)) return false;

                    // 1. Exact route match
                    if (path.Equals(cleanPath, StringComparison.OrdinalIgnoreCase) ||
                        path.Equals(cleanPath + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    // 2. Sub-path match (kailangan may '/' boundary at naka-enable ang catch-all)
                    if (hasCatchAll && path.StartsWith(cleanPath + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    return false;
                });
            }

            if (endpoint == null)
            {
                await BlockRequest(context, 404, "Not Found: Unregistered or inactive API route.", traceId);
                return;
            }

            context.Items["MatchedEndpoint"] = endpoint;
            // =========================================================================
            // PHASE 2.5: HTTP METHOD VALIDATION (BAGONG DAGDAG)
            // =========================================================================
            string incomingMethod = context.Request.Method.ToUpper();
            string rawAllowedMethods = !string.IsNullOrWhiteSpace(endpoint.AllowedMethods)
                ? endpoint.AllowedMethods
                : endpoint.UpstreamHttpMethod;

            if (!string.IsNullOrWhiteSpace(rawAllowedMethods))
            {
                var allowedMethods = rawAllowedMethods
                    .Split(new[] { ',', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => m.Trim().ToUpper())
                    .ToList();

                if (!allowedMethods.Contains(incomingMethod))
                {
                    await BlockRequest(context, 405, $"405 Method Not Allowed: HTTP {incomingMethod} request is not allowed for route '{endpoint.Code}'", traceId);
                    return;
                }
            }
            // PHASE 3: EDGE SECURITY (Operating Hours & IP Restriction Rules)
            if (!Common.IsEndpointTimeValid(endpoint.DateFrom, endpoint.DateTo, endpoint.TimeFrom, endpoint.TimeTo, endpoint.AllowedDays))
            {
                await BlockRequest(context, 403, "Access Denied: Endpoint is currently outside operating hours.", traceId);
                return;
            }

            if (endpoint.IpRules != null && endpoint.IpRules.Count > 0)
            {
                var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "";
                bool isBlocked = endpoint.IpRules.Any(r => r.RuleType.Equals("Deny", StringComparison.OrdinalIgnoreCase) && r.IpAddressOrRange == clientIp);
                bool hasAllowRules = endpoint.IpRules.Any(r => r.RuleType.Equals("Allow", StringComparison.OrdinalIgnoreCase));
                bool isAllowed = !isBlocked && (!hasAllowRules || endpoint.IpRules.Any(r => r.RuleType.Equals("Allow", StringComparison.OrdinalIgnoreCase) && r.IpAddressOrRange == clientIp));

                if (!isAllowed)
                {
                    await BlockRequest(context, 403, "Access denied by IP security policy.", traceId);
                    return;
                }
            }

            // PHASE 4: INTEGRATION BRANCHING

            // --- BRANCH A: INTERNAL AUTH (Token Generator) ---
            if (endpoint.IntegrationType.Equals("INTERNAL_AUTH", StringComparison.OrdinalIgnoreCase))
            {
                var (isSuccess, jsonResponse, statusCode) = await authService.ProcessInternalAuthAsync(context, endpoint);

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(jsonResponse);
                return;
            }

            // --- BRANCH B: MOCK SERVICE ---
            if (endpoint.IntegrationType.Equals("MOCK", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = endpoint.MockResponseCode ?? 200;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(endpoint.MockResponseBody ?? "{\"message\": \"Mock Success\"}");
                return;
            }

            // --- BRANCH C: REVERSE PROXY ---
            if (endpoint.IntegrationType.Equals("PROXY", StringComparison.OrdinalIgnoreCase))
            {
                // 1. API Key Check
                if (endpoint.RequireApiKey)
                {
                    string? apiKey = GetHeaderValue(context.Request, "X-Api-Key", "X-Client-Id", "X-External-Api-Key");

                    if (string.IsNullOrWhiteSpace(apiKey))
                    {
                        await BlockRequest(context, 401, "Unauthorized: X-Api-Key header is required.", traceId);
                        return;
                    }

                    var client = await apiClientService.ByApiKey(apiKey);

                    if (client == null || string.IsNullOrEmpty(client.Code))
                    {
                        await BlockRequest(context, 401, "Unauthorized: Invalid API Key.", traceId);
                        return;
                    }

                    context.Request.Headers["X-Client-Id"] = client.Id.ToString();
                    context.Items["MatchedClient"] = client;

                    if (client.SSLRequired && !certValidator.Validate(context.Connection.ClientCertificate))
                    {
                        await BlockRequest(context, 403, "Valid Client Certificate Required.", traceId);
                        return;
                    }

                    if (context.Request.Headers.ContainsKey("X-Signature"))
                    {
                        string sigResult = await ValidateRequest.SignatureAsync(context, client);
                        if (!sigResult.Equals("Valid", StringComparison.OrdinalIgnoreCase))
                        {
                            await BlockRequest(context, 401, sigResult, traceId);
                            return;
                        }
                    }
                }

                // 2. JWT Token Enforcement Check
                if (endpoint.AuthProviderId.HasValue && endpoint.AuthProvider != null)
                {
                    if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) ||
                        !authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        await BlockRequest(context, 401, "Unauthorized: Bearer JWT Token is required for this endpoint.", traceId);
                        return;
                    }

                    string token = authHeader.ToString().Substring("Bearer ".Length).Trim();
                    bool isJwtValid = JwtHelper.ValidateToken(token, endpoint.AuthProvider);

                    if (!isJwtValid)
                    {
                        await BlockRequest(context, 401, "Unauthorized: Invalid, expired, or signature mismatched JWT token.", traceId);
                        return;
                    }
                }
            }

            // Remove compressed headers so Proxy response is readable in logs
            context.Request.Headers.Remove("Accept-Encoding");

            await next(context);
        }

        private static string? GetHeaderValue(HttpRequest request, params string[] possibleKeys)
        {
            foreach (var key in possibleKeys)
            {
                if (request.Headers.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                {
                    return val.ToString().Trim();
                }
            }
            return null;
        }

        private static async Task BlockRequest(HttpContext context, int statusCode, string message, string traceId)
        {
            context.Response.StatusCode = statusCode;

            if (!context.Response.HasStarted)
            {
                context.Response.Headers["X-TraceID"] = traceId;
                context.Response.ContentType = "application/json";
            }

            await context.Response.WriteAsync($"{{\"error\": \"Blocked\", \"message\": \"{message}\"}}");
        }
    }
}