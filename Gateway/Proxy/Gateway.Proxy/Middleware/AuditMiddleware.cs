using Gateway.BLL.Services.IServices;
using Gateway.Data.Models;
using Microsoft.AspNetCore.Http.Extensions;
using System.IO.Compression;
using System.Text;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.Proxy.Middleware
{
    public class AuditMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context, ILogService logService)
        {
            string traceId = context.TraceIdentifier;
            context.Request.Headers["X-TraceID"] = traceId;

            var route = context.Items["MatchedRoute"] as CustomFileRoute;
            var client = context.Items["MatchedClient"] as Response.Client;

            context.Request.EnableBuffering();
            var formattedRequest = await FormatRequest(context.Request);

            logService.LogHttp(new HttpLog
            {
                ClientId = client?.Id,
                RouteId = route?.Id ?? "",
                TraceId = traceId,
                HttpMethod = context.Request.Method,
                Uri = context.Request.GetDisplayUrl(),
                HttpVersion = context.Request.Protocol,
                Referrer = context.Connection.RemoteIpAddress?.ToString(),
                RequestData = formattedRequest,
                RequestDate = DateTime.Now,
                UserAgent = context.Request.Headers["User-Agent"].ToString()
            }, "REQUEST");

            var originalBodyStream = context.Response.Body;
            using var memStream = new MemoryStream();
            context.Response.Body = memStream;

            await next(context);

            
            var responseData = await FormatResponse(context.Response);

            logService.LogHttp(new HttpLog
            {
                ClientId = client?.Id,
                TraceId = traceId,
                RouteId = route?.Id ?? "",
                ResponseCode = context.Response.StatusCode.ToString(),
                ResponseData = responseData,
                ResponseDate = DateTime.Now
            }, "RESPONSE");
            

            context.Response.Headers["X-TraceID"] = traceId;
            memStream.Position = 0;
            await memStream.CopyToAsync(originalBodyStream);
        }

        private async Task<string> FormatRequest(HttpRequest request)
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
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                request.Body.Position = 0;
                builder.AppendLine("  \"Body\": " + (string.IsNullOrEmpty(body) ? "\"[Empty Body]\"" : body));
            }
            builder.AppendLine("}");
            return builder.ToString();
        }

        private async Task<string> FormatResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            string text = "";
            if (response.Headers["Content-Encoding"].ToString().Contains("gzip"))
            {
                var bytes = await ReadFullyAsync(response.Body);
                var decompressed = Decompress(bytes);
                text = Encoding.UTF8.GetString(decompressed);
            }
            else
            {
                using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
                text = await reader.ReadToEndAsync();
            }
            response.Body.Seek(0, SeekOrigin.Begin);
            return text;
        }

        private static async Task<byte[]> ReadFullyAsync(Stream input)
        {
            using var ms = new MemoryStream();
            await input.CopyToAsync(ms);
            return ms.ToArray();
        }

        private static byte[] Decompress(byte[] data)
        {
            using var compressedStream = new MemoryStream(data);
            using var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();
            zipStream.CopyTo(resultStream);
            return resultStream.ToArray();
        }
    }
}