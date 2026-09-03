using Gateway.BLL.Helper;
using Gateway.BLL.Services;
using Gateway.BLL.Services.IService;
using Gateway.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
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

        if (path.Equals("/health", StringComparison.OrdinalIgnoreCase) || path.EndsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        bool isAuthenticateRoute = path.Contains("authenticate", StringComparison.OrdinalIgnoreCase);
        Response.Client? client = null;

        if (isAuthenticateRoute)
        {
            context.Request.EnableBuffering();
            string clientCode = string.Empty;
            string clientSecret = string.Empty;

            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                var bodyText = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (!string.IsNullOrWhiteSpace(bodyText))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(bodyText);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("code", out var cElement) || root.TryGetProperty("client_id", out cElement) || root.TryGetProperty("username", out cElement)) clientCode = cElement.GetString() ?? "";
                        if (root.TryGetProperty("secret", out var sElement) || root.TryGetProperty("password", out sElement) || root.TryGetProperty("api_key", out sElement)) clientSecret = sElement.GetString() ?? "";
                    }
                    catch (JsonException)
                    {
                        await BlockRequest(context, 400, "Invalid JSON request payload.", traceId);
                        return;
                    }
                }
            }

            if (string.IsNullOrEmpty(clientCode) || string.IsNullOrEmpty(clientSecret))
            {
                await BlockRequest(context, 400, "Client Code and Secret are required.", traceId);
                return;
            }

            client = await apiClientService.ByUsernameAndPassword(clientCode, clientSecret);

            if (client == null || string.IsNullOrEmpty(client.Code))
            {
                await BlockRequest(context, 401, "Invalid client credentials.", traceId);
                return;
            }

            context.Request.Headers["X-Client-Id"] = client.Id;
            context.Items["MatchedClient"] = client;
            await next(context);
            return;
        }

        var activeEndpoints = await dbContext.Set<ApiEndpoint>()
            .Include(e => e.IpRules)
            .Where(e => e.IsActive)
            .AsNoTracking()
            .ToListAsync();

        var endpoint = activeEndpoints.FirstOrDefault(e =>
        {
            var basePath = e.UpstreamPathTemplate
                .Replace("{**catch-all}", "", StringComparison.OrdinalIgnoreCase)
                .Replace("{*catch-all}", "", StringComparison.OrdinalIgnoreCase)
                .TrimEnd('/');

            if (string.IsNullOrEmpty(basePath)) return false;

            return path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase) || path.Equals(basePath, StringComparison.OrdinalIgnoreCase);
        });

        if (endpoint != null)
        {
            context.Items["MatchedEndpoint"] = endpoint;

            bool isTimeValid = Common.IsEndpointTimeValid(endpoint.DateFrom, endpoint.DateTo, endpoint.TimeFrom, endpoint.TimeTo, endpoint.AllowedDays);
            if (!isTimeValid)
            {
                await BlockRequest(context, 403, "Access Denied: Endpoint is currently outside of its operating hours or active dates.", traceId);
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
        }

        bool requireApiKey = endpoint?.RequireApiKey ?? true;

        if (requireApiKey)
        {
            context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey);
            client = await apiClientService.ByApiKey(apiKey!);

            if (client == null || string.IsNullOrEmpty(client.Code))
            {
                await BlockRequest(context, 401, "Invalid or missing API Key.", traceId);
                return;
            }

            context.Request.Headers["X-Client-Id"] = client.Id;
            context.Items["MatchedClient"] = client;

            if (client.SSLRequired && !certValidator.Validate(context.Connection.ClientCertificate))
            {
                await BlockRequest(context, 403, "Valid Client Certificate Required.", traceId);
                return;
            }

            if (context.Request.Headers.ContainsKey("X-Signature"))
            {
                string sigValidationResult = await ValidateRequest.SignatureAsync(context, client);
                if (!sigValidationResult.Equals("Valid", StringComparison.OrdinalIgnoreCase))
                {
                    await BlockRequest(context, 401, sigValidationResult, traceId);
                    return;
                }
            }
        }

        await next(context);
    }

    private static async Task BlockRequest(HttpContext context, int statusCode, string message, string traceId)
    {
        context.Response.StatusCode = statusCode;
        context.Response.Headers["X-TraceID"] = traceId;
        await context.Response.WriteAsync(message);
    }
}