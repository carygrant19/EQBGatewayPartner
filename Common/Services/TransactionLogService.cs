using Common.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Common.Services
{
    public class TransactionLogService : ITransactionLogService
    {
        private readonly string _conString;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TransactionLogService> _logger;

        public TransactionLogService(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<TransactionLogService> logger)
        {
            _conString = configuration.GetConnectionString("GatewayPartner")
                      ?? configuration.GetConnectionString("DefaultConnection")
                      ?? configuration["ConnectionStrings:Temenos"]!;

            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<long> LogRequestAsync(string? transactionId, string urlEndpoint, string requestPayload, DateTime requestDate)
        {
            string traceId = GetCurrentTraceId();

            const string sql = @"
                INSERT INTO [dbo].[Log_Transaction]
                (
                    [TransactionId], [TraceId], [UrlEndpoint], 
                    [Request], [RequestDate], [Status]
                )
                OUTPUT INSERTED.[Id]
                VALUES
                (
                    @TransactionId, @TraceId, @UrlEndpoint, 
                    @Request, @RequestDate, 'PENDING'
                );";

            try
            {
                using SqlConnection con = new(_conString);
                using SqlCommand cmd = new(sql, con);

                cmd.Parameters.Add("@TransactionId", SqlDbType.VarChar, 100).Value = string.IsNullOrWhiteSpace(transactionId) ? DBNull.Value : transactionId;
                cmd.Parameters.Add("@TraceId", SqlDbType.VarChar, 100).Value = (object?)traceId ?? DBNull.Value;
                cmd.Parameters.Add("@UrlEndpoint", SqlDbType.VarChar, 500).Value = (object?)urlEndpoint ?? DBNull.Value;
                cmd.Parameters.Add("@Request", SqlDbType.NVarChar, -1).Value = (object?)requestPayload ?? DBNull.Value;
                cmd.Parameters.Add("@RequestDate", SqlDbType.DateTime2).Value = requestDate;

                await con.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt64(result);
            }
            catch (Exception ex)
            {
                await LogExceptionAsync("TransactionLogService.LogRequest", ex);
                return 0;
            }
        }

        public async Task LogResponseAsync(long logId, string responsePayload, DateTime responseDate, string status)
        {
            if (logId <= 0) return;

            try
            {
                string checksum = ComputeCheckSum($"{logId}|{responsePayload}|{status}");

                const string sql = @"
                    UPDATE [dbo].[Log_Transaction]
                    SET 
                        [Response] = @Response,
                        [ResponseDate] = @ResponseDate,
                        [Status] = @Status,
                        [CheckSum] = @CheckSum
                    WHERE [Id] = @Id;";

                using SqlConnection con = new(_conString);
                using SqlCommand cmd = new(sql, con);

                cmd.Parameters.Add("@Id", SqlDbType.BigInt).Value = logId;
                cmd.Parameters.Add("@Response", SqlDbType.NVarChar, -1).Value = (object?)responsePayload ?? DBNull.Value;
                cmd.Parameters.Add("@ResponseDate", SqlDbType.DateTime2).Value = responseDate;
                cmd.Parameters.Add("@Status", SqlDbType.VarChar, 50).Value = (object?)status ?? DBNull.Value;
                cmd.Parameters.Add("@CheckSum", SqlDbType.VarChar, 64).Value = (object?)checksum ?? DBNull.Value;

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                await LogExceptionAsync("TransactionLogService.LogResponse", ex);
            }
        }

        public async Task LogExceptionAsync(string moduleName, Exception ex)
        {
            try
            {
                const string sql = @"
                    INSERT INTO [dbo].[Log_Exception]
                    (
                        [ModuleName], [Message], [Source], 
                        [InnerException], [StackTrace], [LogDate]
                    )
                    VALUES
                    (
                        @ModuleName, @Message, @Source, 
                        @InnerException, @StackTrace, @LogDate
                    );";

                using SqlConnection con = new(_conString);
                using SqlCommand cmd = new(sql, con);

                string safeModuleName = string.IsNullOrWhiteSpace(moduleName)
                    ? "UNKNOWN"
                    : (moduleName.Length > 50 ? moduleName[..50] : moduleName);

                cmd.Parameters.Add("@ModuleName", SqlDbType.VarChar, 50).Value = safeModuleName;
                cmd.Parameters.Add("@Message", SqlDbType.Text).Value = ex.Message ?? string.Empty;
                cmd.Parameters.Add("@Source", SqlDbType.Text).Value = ex.Source ?? string.Empty;
                cmd.Parameters.Add("@InnerException", SqlDbType.NVarChar, -1).Value = ex.InnerException?.ToString() ?? string.Empty;
                cmd.Parameters.Add("@StackTrace", SqlDbType.NVarChar, -1).Value = ex.StackTrace ?? string.Empty;
                cmd.Parameters.Add("@LogDate", SqlDbType.DateTime).Value = DateTime.Now;

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Critical: Failed to insert into [EQBGatewayPartner].[dbo].[Log_Exception]");
            }
        }

        public string GetCurrentTraceId()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null && context.Request.Headers.TryGetValue("X-TraceID", out var headerTraceId) && !string.IsNullOrWhiteSpace(headerTraceId))
            {
                return headerTraceId.ToString();
            }
            return context?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
        }

        private static string ComputeCheckSum(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }
    }
}