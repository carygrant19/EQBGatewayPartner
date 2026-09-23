using Gateway.BLL.Helper;
using Gateway.BLL.Services;
using Gateway.BLL.Services.IService;
using Gateway.Data.Models;
using Gateway.Proxy.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Model;
using Model = Gateway.Data.Models;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.Proxy.Middleware
{
    public class SecurityMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(
            HttpContext context,
            IClientService apiClientService,
            ICertificateValidatorService certValidator,
            IAuthService authService,
            IConfiguration configuration,
            EFDbContext dbContext,
            IMemoryCache cache)
        {
            string traceId = context.TraceIdentifier;
            var path = context.Request.Path.Value ?? "";

            // PHASE 1: INTERNAL EXEMPTIONS (/health, /reload)
            if (path.Equals("/health", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("/health", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("/internal/gateway/reload", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            // PHASE 2: ROUTE CACHE WITH EAGER LOADING
            var activeEndpoints = await cache.GetOrCreateAsync("GATEWAY_ACTIVE_ROUTES", async entry =>
            {
                entry.AddExpirationToken(GatewayCacheSignal.GetToken());
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

                return await dbContext.Set<Model.Route>()
                    .Include(e => e.TargetHosts)
                    .Include(e => e.IpRules)
                    .Include(e => e.AuthProvider)
                    .Include(e => e.Transforms)
                    .Include(e => e.ClientRouteAccess)
                    .Include(e => e.OutboundAuthProfile)
                        .ThenInclude(p => p.Headers)
                    .Where(e => e.IsActive)
                    .AsNoTracking()
                    .ToListAsync();
            }) ?? new List<Model.Route>();

            var routeModel = context.GetEndpoint()?.Metadata.GetMetadata<RouteModel>();
            string? routeCode = routeModel?.Config.RouteId;

            Model.Route? endpoint = null;

            if (!string.IsNullOrEmpty(routeCode))
            {
                endpoint = activeEndpoints.FirstOrDefault(e => e.Code == routeCode);
            }

            endpoint ??= activeEndpoints.FirstOrDefault(e =>
            {
                bool hasCatchAll = e.UpstreamPathTemplate.Contains("{**catch-all}", StringComparison.OrdinalIgnoreCase)
                                || e.UpstreamPathTemplate.Contains("{**remainder}", StringComparison.OrdinalIgnoreCase);

                var cleanPath = RouteTemplateHelper.StripWildcards(e.UpstreamPathTemplate).TrimEnd('/');

                if (string.IsNullOrEmpty(cleanPath)) return false;

                if (path.Equals(cleanPath, StringComparison.OrdinalIgnoreCase) || path.Equals(cleanPath + "/", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (hasCatchAll && path.StartsWith(cleanPath + "/", StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            });

            if (endpoint == null)
            {
                await BlockRequest(context, 404, "Not Found: Unregistered or inactive API route.", traceId);
                return;
            }

            context.Items["MatchedEndpoint"] = endpoint;
            bool isInternalAuth = endpoint.IntegrationType.Equals("INTERNAL_AUTH", StringComparison.OrdinalIgnoreCase);

            // PHASE 3: EDGE SECURITY (Methods, Operating Hours, IP)
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

            // PHASE 4: CLIENT IDENTIFICATION, mTLS, & ROUTE ACCESS
            string? apiKey = GetHeaderValue(context.Request, "X-Api-Key", "X-Client-Id", "X-External-Api-Key");

            if (endpoint.RequireApiKey && string.IsNullOrWhiteSpace(apiKey))
            {
                await BlockRequest(context, 401, "Unauthorized: X-Api-Key header is required.", traceId);
                return;
            }

            Response.Client? client = null;

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client = await apiClientService.ByApiKey(apiKey);

                if (client == null || string.IsNullOrEmpty(client.Code))
                {
                    await BlockRequest(context, 401, "Unauthorized: Invalid API Key.", traceId);
                    return;
                }

                // SSL/mTLS Check kung SSLRequired = true at hindi INTERNAL_AUTH
                if (client.SSLRequired && !isInternalAuth)
                {
                    var clientCert = await context.Connection.GetClientCertificateAsync();
                    if (clientCert == null || !certValidator.Validate(clientCert))
                    {
                        await BlockRequest(context, 403, "Access Denied: Valid Client Certificate (mTLS) is required for this API Key.", traceId);
                        return;
                    }
                }

                if (endpoint.RequireApiKey)
                {
                    int parsedClientId = int.Parse(client.Id);
                    bool hasRouteAccess = endpoint.ClientRouteAccess != null &&
                        endpoint.ClientRouteAccess.Any(a => a.ClientId == parsedClientId && a.IsAllowed);

                    if (!hasRouteAccess)
                    {
                        await BlockRequest(context, 403, "Forbidden: Client does not have access permission to this route.", traceId);
                        return;
                    }
                }

                context.Request.Headers["X-Client-Id"] = client.Id.ToString();
                context.Items["MatchedClient"] = client;
            }

            // PHASE 5: JWT AUTHENTICATION (Exempted kung INTERNAL_AUTH)
            if (endpoint.AuthProviderId.HasValue && !isInternalAuth)
            {
                if (context.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
                    authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    string token = authHeader.ToString().Substring("Bearer ".Length).Trim();
                    Model.AuthProvider? targetAuthProvider = endpoint.AuthProvider;

                    targetAuthProvider ??= new Model.AuthProvider
                    {
                        Code = "GATEWAY",
                        Name = "Gateway",
                        Issuer = configuration["JWT:Issuer"] ?? "",
                        Audience = configuration["JWT:Audience"] ?? "",
                        SecretKey = configuration["JWT:Secret"] ?? "",
                        IsActive = true
                    };

                    if (!JwtHelper.ValidateToken(token, targetAuthProvider))
                    {
                        await BlockRequest(context, 401, "Unauthorized: Invalid or expired Bearer JWT token.", traceId);
                        return;
                    }
                }
                else
                {
                    await BlockRequest(context, 401, "Unauthorized: Authorization Bearer token is missing.", traceId);
                    return;
                }
            }

            // PHASE 6: INTEGRATION BRANCHING
            if (isInternalAuth)
            {
                var (isSuccess, jsonResponse, statusCode) = await authService.ProcessInternalAuthAsync(context, endpoint);
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(jsonResponse);
                return;
            }

            if (endpoint.IntegrationType.Equals("MOCK", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = endpoint.MockResponseCode ?? 200;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(endpoint.MockResponseBody ?? "{\"message\": \"Mock Success\"}");
                return;
            }

            if (endpoint.IntegrationType.Equals("PROXY", StringComparison.OrdinalIgnoreCase))
            {
                // OUTBOUND AUTH HEADERS INJECTION
                if (endpoint.OutboundAuthProfile != null && endpoint.OutboundAuthProfile.Headers != null)
                {
                    foreach (var authHeaderItem in endpoint.OutboundAuthProfile.Headers)
                    {
                        string targetHeaderName = string.IsNullOrWhiteSpace(authHeaderItem.HeaderName) ? "Authorization" : authHeaderItem.HeaderName;
                        string formattedValue = authHeaderItem.AuthType.Equals("Bearer", StringComparison.OrdinalIgnoreCase)
                            ? $"Bearer {authHeaderItem.CredentialValue}"
                            : authHeaderItem.CredentialValue;

                        context.Request.Headers[targetHeaderName] = formattedValue;
                    }
                }
            }

            context.Request.Headers.Remove("Accept-Encoding");
            await next(context);
        }

        private static string? GetHeaderValue(HttpRequest request, params string[] possibleKeys)
        {
            foreach (var key in possibleKeys)
            {
                if (request.Headers.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                    return val.ToString().Trim();
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