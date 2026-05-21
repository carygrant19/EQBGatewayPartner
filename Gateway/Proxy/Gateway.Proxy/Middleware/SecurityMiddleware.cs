using Gateway.BLL.Helper;
using Gateway.BLL.Services;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Gateway.Data.Models;
using Microsoft.Extensions.Options;

namespace Gateway.Proxy.Middleware;

public class SecurityMiddleware(RequestDelegate next, ILogger<SecurityMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context,
        IApiClientService apiClientService,
        ICertificateValidatorService certValidator,
        IOptionsMonitor<OcelotCustomFileConfiguration> config,
        ILogService logService)
    {
        string traceId = context.TraceIdentifier;

        context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey);
        var client = await apiClientService.ByApiKey(apiKey!);

        if (client != null)
        {
            context.Items["MatchedClient"] = client;
        }

        if (context.Request.Path.Value!.Contains("authenticate", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (client == null || string.IsNullOrEmpty(client.Username))
        {
            await BlockRequest(context, 401, "Invalid or missing API Key.", traceId);
            return;
        }

        if (client.SSLRequired && !certValidator.Validate(context.Connection.ClientCertificate))
        {
            await BlockRequest(context, 403, "Valid Client Certificate Required.", traceId);
            return;
        }

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