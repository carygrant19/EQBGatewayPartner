using Common.Services.IService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Handlers
{
    public class OutboundLoggingHandler : DelegatingHandler
    {
        private readonly ITransactionLogService _transactionLogService;

        public OutboundLoggingHandler(ITransactionLogService transactionLogService)
        {
            _transactionLogService = transactionLogService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestDate = DateTime.Now;
            string traceId = _transactionLogService.GetCurrentTraceId();

            if (!request.Headers.Contains("X-TraceID"))
            {
                request.Headers.Add("X-TraceID", traceId);
            }

            string? transactionId = null;
            if (request.Headers.TryGetValues("uniqueIdentifier", out var uIdValues))
            {
                transactionId = string.Join(",", uIdValues);
            }

            string endpointUrl = request.RequestUri?.ToString() ?? string.Empty;
            string formattedRequest = await FormatRequestPayloadAsync(request, cancellationToken);

            long logId = await _transactionLogService.LogRequestAsync(
                transactionId,
                endpointUrl,
                formattedRequest,
                requestDate
            );

            HttpResponseMessage? response = null;
            string formattedResponse = string.Empty;
            string status = "FAILED";

            try
            {
                response = await base.SendAsync(request, cancellationToken);
                status = response.IsSuccessStatusCode ? "SUCCESS" : "FAILED";
                formattedResponse = await FormatResponsePayloadAsync(response, cancellationToken);
                return response;
            }
            catch (Exception ex)
            {
                status = "ERROR";
                formattedResponse = JsonConvert.SerializeObject(new
                {
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                }, Formatting.Indented);

                await _transactionLogService.LogExceptionAsync("OutboundLoggingHandler", ex);
                throw;
            }
            finally
            {
                await _transactionLogService.LogResponseAsync(
                    logId,
                    formattedResponse,
                    DateTime.Now,
                    status
                );
            }
        }

        private static async Task<string> FormatRequestPayloadAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var headers = new Dictionary<string, string>();
            foreach (var header in request.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }

            string rawBody = string.Empty;
            if (request.Content != null)
            {
                foreach (var header in request.Content.Headers)
                {
                    headers[header.Key] = string.Join(", ", header.Value);
                }
                rawBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            var payload = new
            {
                Method = request.Method.Method,
                Headers = headers,
                Body = ParseBody(rawBody)
            };

            return JsonConvert.SerializeObject(payload, Formatting.Indented);
        }

        private static async Task<string> FormatResponsePayloadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var headers = new Dictionary<string, string>();
            foreach (var header in response.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }

            string rawBody = string.Empty;
            if (response.Content != null)
            {
                foreach (var header in response.Content.Headers)
                {
                    headers[header.Key] = string.Join(", ", header.Value);
                }
                rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
            }

            var payload = new
            {
                StatusCode = (int)response.StatusCode,
                Headers = headers,
                Body = ParseBody(rawBody)
            };

            return JsonConvert.SerializeObject(payload, Formatting.Indented);
        }

        private static object ParseBody(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return "[Empty Body]";
            }

            try
            {
                return JsonConvert.DeserializeObject(content) ?? content;
            }
            catch
            {
                return content;
            }
        }
    }
}