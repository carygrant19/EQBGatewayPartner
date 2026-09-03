using Gateway.BLL.Helper;
using Gateway.BLL.Services;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Gateway.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using Response = Gateway.BLL.DTO.Response;
namespace Gateway.Proxy.Middleware;

public class SecurityMiddleware(RequestDelegate next, ILogger<SecurityMiddleware> logger)
{
    public async Task InvokeAsync(
        HttpContext context,
        IClientService apiClientService,
        ICertificateValidatorService certValidator,
        IOptionsMonitor<OcelotCustomFileConfiguration> config,
        ILogService logService)
    {
        string traceId = context.TraceIdentifier;

        // 1. ADD THIS BYPASS CHECK FOR THE HEALTH ENDPOINT
        bool isHealthRoute = context.Request.Path.Value!.Equals("/health", StringComparison.OrdinalIgnoreCase)
                          || context.Request.Path.Value!.EndsWith("/health", StringComparison.OrdinalIgnoreCase);

        if (isHealthRoute)
        {
            await next(context); // Skip everything and pass directly to the health check controller
            return;
        }

        bool isAuthenticateRoute = context.Request.Path.Value!.Contains("authenticate", StringComparison.OrdinalIgnoreCase);

        Response.Client? client = null;

        // =========================================================
        // 1. ROUTE: /authenticate (Validation via Username/Password)
        // =========================================================
        if (isAuthenticateRoute)
        {
            // Kinakailangan ang Buffering para mabasa ang body sa middleware at mabasa pa rin downstream ni Ocelot
            context.Request.EnableBuffering();

            string username = string.Empty;
            string password = string.Empty;

            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                var bodyText = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0; // I-reset ang stream position para mabasa pa ng downstream controllers

                if (!string.IsNullOrWhiteSpace(bodyText))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(bodyText);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("username", out var uElement)) username = uElement.GetString() ?? "";
                        if (root.TryGetProperty("password", out var pElement)) password = pElement.GetString() ?? "";
                    }
                    catch (JsonException)
                    {
                        await BlockRequest(context, 400, "Invalid JSON request payload.", traceId);
                        return;
                    }
                }
            }

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                await BlockRequest(context, 400, "Username and Password are required.", traceId);
                return;
            }

            // Hanapin si Client gamit ang Username at Password
            client = await apiClientService.ByUsernameAndPassword(username, password);

            if (client == null)
            {
                await BlockRequest(context, 401, "Invalid client username or password.", traceId);
                return;
            }

            // Automatic Injection para hindi mag-error si Ocelot Rate Limiter
            context.Request.Headers["X-Client-Id"] = client.Id.ToString();
            context.Items["MatchedClient"] = client;

            await next(context);
            return;
        }

        // =========================================================
        // 2. OTHER ROUTES (Validation via X-Api-Key)
        // =========================================================
        context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey);
        client = await apiClientService.ByApiKey(apiKey!);

        if (client == null || string.IsNullOrEmpty(client.Username))
        {
            await BlockRequest(context, 401, "Invalid or missing API Key.", traceId);
            return;
        }

        // Automatic Injection ng Client ID
        context.Request.Headers["X-Client-Id"] = client.Id.ToString();
        context.Items["MatchedClient"] = client;

        // SSL Certificate Check
        if (client.SSLRequired && !certValidator.Validate(context.Connection.ClientCertificate))
        {
            await BlockRequest(context, 403, "Valid Client Certificate Required.", traceId);
            return;
        }

        // Route Authorization & Schedule Checks
        var route = config.CurrentValue.Routes.FirstOrDefault(r =>
            context.Request.Path.Value.Contains(r.UpstreamPathTemplate, StringComparison.OrdinalIgnoreCase));

        if (route != null)
        {
            context.Items["MatchedRoute"] = route;
            string rMessage = "";
            bool authorized = true;

            if (route.TimeLimit != null && route.TimeLimit.EnableTimeLimit)
            {
                int currentDay = (int)DateTime.Now.DayOfWeek;

                if (route.TimeLimit.AllowedDays != null && route.TimeLimit.AllowedDays.Count > 0)
                {
                    if (!route.TimeLimit.AllowedDays.Contains(currentDay))
                    {
                        authorized = false;
                        rMessage = "Access denied: Not allowed on this day of the week.";
                    }
                }

                if (authorized)
                {
                    TimeSpan.TryParse(route.TimeLimit.TimeFrom, out TimeSpan start);
                    TimeSpan.TryParse(route.TimeLimit.TimeTo, out TimeSpan end);

                    if (!Common.IsWithinTimeLimit(start, end))
                    {
                        authorized = false;
                        rMessage = "Access denied: Outside of allowed time range.";
                    }
                }
            }

            if (authorized && route.RequireSignature)
            {
                if (!Common.ValidateRequestMethodAndSignature(context, client, out string sigMessage))
                {
                    authorized = false;
                    rMessage = sigMessage;
                }
            }

            if (!authorized)
            {
                await BlockRequest(context, 401, rMessage, traceId);
                return;
            }
        }

        await next(context);
    }

    private async Task BlockRequest(HttpContext context, int statusCode, string message, string traceId)
    {
        context.Response.StatusCode = statusCode;
        context.Response.Headers["X-TraceID"] = traceId;
        await context.Response.WriteAsync(message);
    }
}