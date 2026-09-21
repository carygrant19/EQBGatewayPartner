using Gateway.BLL.Services.IServices;
using Gateway.Data.Models;
using Microsoft.AspNetCore.Http.Extensions;
using System.Text;
using Response = Gateway.BLL.DTO.Response;
using Model = Gateway.Data.Models;

namespace Gateway.Proxy.Middleware
{
    public class AuditMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context, ILogService logService)
        {
            string traceId = context.TraceIdentifier;
            context.Request.Headers["X-TraceID"] = traceId;

            if (!context.Response.HasStarted && !context.Response.Headers.ContainsKey("X-TraceID"))
            {
                context.Response.Headers["X-TraceID"] = traceId;
            }
             
            context.Request.EnableBuffering();
            var formattedRequest = await FormatRequest(context.Request);
            var requestDate = DateTime.Now;
             
            logService.LogHttp(new HttpLog
            {
                TraceId = traceId,
                HttpMethod = context.Request.Method,
                Uri = context.Request.GetDisplayUrl(),
                HttpVersion = context.Request.Protocol,
                Referrer = context.Connection.RemoteIpAddress?.ToString(),
                RequestData = formattedRequest,
                RequestDate = requestDate,
                UserAgent = context.Request.Headers["User-Agent"].ToString()
            }, "REQUEST");
             
            var originalBodyStream = context.Response.Body;
            using var memStream = new MemoryStream();
            context.Response.Body = memStream;

            try
            { 
                await next(context);
            }
            finally
            { 
                var endpoint = context.Items["MatchedEndpoint"] as Model.Route;
                var clientObj = context.Items["MatchedClient"];
                var responseData = await FormatResponse(memStream);
                 
                logService.LogHttp(new HttpLog
                {
                    ClientId = ExtractClientId(clientObj),
                    RouteId = endpoint?.Id.ToString() ?? "",
                    TraceId = traceId,
                    ResponseCode = context.Response.StatusCode.ToString(),
                    ResponseData = responseData,
                    ResponseDate = DateTime.Now
                }, "RESPONSE");
                 
                if (memStream.CanRead && memStream.CanSeek && memStream.Length > 0)
                {
                    memStream.Position = 0;
                    await memStream.CopyToAsync(originalBodyStream);
                }

                context.Response.Body = originalBodyStream;
            }
        }

        private static string? ExtractClientId(object? clientObj)
        {
            return clientObj switch
            {
                Response.Client dtoClient => dtoClient.Id,
                Client entityClient => entityClient.Id.ToString(),
                _ => null
            };
        }

        private static async Task<string> FormatRequest(HttpRequest request)
        {
            request.EnableBuffering();
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"Headers\": {");
            var headers = request.Headers.Select(h => $"    \"{h.Key}\": \"{h.Value}\"");
            builder.AppendLine(string.Join("," + Environment.NewLine, headers));
            builder.AppendLine("  },");

            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync();
                builder.AppendLine("  \"FormData\": {");
                var fields = form.Keys.Select(k => $"    \"{k}\": \"{form[k]}\"");
                builder.AppendLine(string.Join("," + Environment.NewLine, fields));
                if (form.Files.Any())
                {
                    builder.AppendLine("    ,\"Files\": [");
                    var files = form.Files.Select(f => $"      {{ \"FileName\": \"{f.FileName}\", \"Size\": {f.Length} }}");
                    builder.AppendLine(string.Join("," + Environment.NewLine, files));
                    builder.AppendLine("    ]");
                }
                builder.AppendLine("  },");
            }
            else
            {
                if (request.Body.CanSeek)
                {
                    request.Body.Position = 0;
                }

                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();

                if (request.Body.CanSeek)
                {
                    request.Body.Position = 0;
                }

                builder.AppendLine("  \"Body\": " + (string.IsNullOrEmpty(body) ? "\"[Empty Body]\"" : body));
            }
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static async Task<string> FormatResponse(MemoryStream memStream)
        {
            try
            {
                if (!memStream.CanSeek || !memStream.CanRead || memStream.Length == 0)
                {
                    return "[Empty Response Body]";
                }

                memStream.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(memStream, Encoding.UTF8, leaveOpen: true);
                var text = await reader.ReadToEndAsync();
                memStream.Seek(0, SeekOrigin.Begin);

                return string.IsNullOrWhiteSpace(text) ? "[Empty Response Body]" : text;
            }
            catch (Exception ex)
            {
                return $"[Error reading response body: {ex.Message}]";
            }
        }
    }
}