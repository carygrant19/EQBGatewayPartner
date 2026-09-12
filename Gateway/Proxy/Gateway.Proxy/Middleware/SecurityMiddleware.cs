using Gateway.BLL.Helper;
using Gateway.BLL.Services;
using Gateway.BLL.Services.IService;
using Gateway.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Yarp.ReverseProxy.Model;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.Proxy.Middleware;

public class SecurityMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IClientService apiClientService,
        ICertificateValidatorService certValidator,
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

        // PHASE 2: ROUTE MATCHING (YARP Metadata or Fallback DB Search)
        var routeModel = context.GetEndpoint()?.Metadata.GetMetadata<RouteModel>();
        string? routeCode = routeModel?.Config.RouteId;

        ApiEndpoint? endpoint = null;

        if (!string.IsNullOrEmpty(routeCode))
        {
            endpoint = await dbContext.Set<ApiEndpoint>()
                .Include(e => e.IpRules)
                .Include(e => e.AuthProvider)
                .Include(e => e.Transforms)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Code == routeCode && e.IsActive);
        }

        if (endpoint == null)
        {
            var activeEndpoints = await dbContext.Set<ApiEndpoint>()
                .Include(e => e.IpRules)
                .Include(e => e.AuthProvider)
                .Include(e => e.Transforms)
                .Where(e => e.IsActive)
                .AsNoTracking()
                .ToListAsync();

            endpoint = activeEndpoints.FirstOrDefault(e =>
            {
                var cleanPath = RouteTemplateHelper.StripWildcards(e.UpstreamPathTemplate);
                return !string.IsNullOrEmpty(cleanPath) &&
                       (path.StartsWith(cleanPath, StringComparison.OrdinalIgnoreCase) || path.Equals(cleanPath, StringComparison.OrdinalIgnoreCase));
            });
        }

        if (endpoint == null)
        {
            await BlockRequest(context, 404, "Not Found: Unregistered or inactive API route.", traceId);
            return;
        }

        context.Items["MatchedEndpoint"] = endpoint;

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

        // --- BRANCH A: EMBEDDED INTERNAL AUTH (Token Generator) ---
        if (endpoint.IntegrationType.Equals("INTERNAL_AUTH", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Headers.TryGetValue("X-External-Api-Key", out var apiKeyHeader);
            context.Request.Headers.TryGetValue("X-External-Api-Secret", out var apiSecretHeader);
            context.Request.Headers.TryGetValue("X-External-Username", out var usernameHeader);
            context.Request.Headers.TryGetValue("X-External-Password", out var passwordHeader);

            string authUser = !string.IsNullOrEmpty(usernameHeader.ToString()) ? usernameHeader.ToString() : apiKeyHeader.ToString();
            string authPass = !string.IsNullOrEmpty(passwordHeader.ToString()) ? passwordHeader.ToString() : apiSecretHeader.ToString();

            if (string.IsNullOrEmpty(authUser) || string.IsNullOrEmpty(authPass))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { message = "Authentication headers missing (X-External-Username/X-External-Password or X-External-Api-Key/X-External-Api-Secret)." });
                return;
            }

            var authClient = await apiClientService.ByUsernameAndPassword(authUser, authPass);
            if (authClient == null || string.IsNullOrEmpty(authClient.Code))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { message = "Invalid client credentials." });
                return;
            }

            if (endpoint.AuthProvider == null || !endpoint.AuthProvider.IsActive)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { message = "System Error: No active Auth Provider mapped to this route." });
                return;
            }

            string token = JwtHelper.GenerateToken(authClient.Code, endpoint.AuthProvider);

            context.Response.StatusCode = 200;
            await context.Response.WriteAsJsonAsync(new
            {
                access_token = token,
                token_type = "Bearer",
                expires_in = endpoint.AuthProvider.TokenLifetimeMinutes * 60,
                client_code = authClient.Code,
                provider = endpoint.AuthProvider.Name
            });

            return; // Short-circuit
        }

        // --- BRANCH B: MOCK SERVICE VIRTUALIZATION ---
        if (endpoint.IntegrationType.Equals("MOCK", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = endpoint.MockResponseCode ?? 200;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(endpoint.MockResponseBody ?? "{\"message\": \"Mock Success\"}");
            return; // Short-circuit
        }

        // --- BRANCH C: REVERSE PROXY ---
        if (endpoint.IntegrationType.Equals("PROXY", StringComparison.OrdinalIgnoreCase))
        {
            // 1. API Key Check
            if (endpoint.RequireApiKey)
            {
                context.Request.Headers.TryGetValue("X-External-Api-Key", out var apiKeyHeader);
                string? apiKey = apiKeyHeader.ToString();

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    await BlockRequest(context, 401, "Unauthorized: X-External-Api-Key header is required.", traceId);
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
        context.Request.Headers.Remove("Accept-Encoding");
        await next(context);
    }

    private static async Task BlockRequest(HttpContext context, int statusCode, string message, string traceId)
    {
        context.Response.StatusCode = statusCode;
        context.Response.Headers["X-TraceID"] = traceId;
        await context.Response.WriteAsync(message);
    }
}