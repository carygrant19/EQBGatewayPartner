using Common.Services.IService;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Temenos.API.Services.IService.v1.casa;
using ApiResponse = Common.DTOs.Response.Api;
using DTORequest = Temenos.API.DTOs.Request.v1.casa;
using DTOResponse = Temenos.API.DTOs.Response.v1.casa;
using ModelRequest = Temenos.API.Models.v1.Request.casa;
using ModelResponse = Temenos.API.Models.v1.Response.casa;

namespace Temenos.API.Services.v1.casa
{
    public class TransactionService(
        IConfiguration configuration,
        ILogger<TransactionService> logger,
        IHttpClientFactory httpClientFactory,
        ITransactionLogService transactionLogService) : ITransactionService
    {
        private readonly ILogger<TransactionService> _logger = logger;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ITransactionLogService _transactionLogService = transactionLogService;
        private readonly string _conString = configuration["ConnectionStrings:Temenos"]!;
        private readonly string _ftEndpoint = configuration["Endpoints:v1_FundTransfer"]!;
        private readonly string _reversalEndpoint = configuration["Endpoints:v1_Reversal"]!;
        private readonly string _scriptPath = Path.Combine($"{Directory.GetCurrentDirectory()}\\scripts");

        public async Task<ApiResponse.Response<DTOResponse.FundTransfer>> FundTransfer(string uId, string companyId, DTORequest.FundTransfer dtoRequest)
        {
            ApiResponse.Response<DTOResponse.FundTransfer> dtoResponse = new();

            try
            {
                ModelRequest.billpay request = new()
                {
                    Body = new ModelRequest.billpayBody
                    {
                        TransactionType = dtoRequest.TransactionType,
                        DebValDate = dtoRequest.DebitValueDate,
                        DebitAmount = dtoRequest.Amount,
                        DebitAccountNumber = dtoRequest.DebitAccountNumber,
                        DebitCurrency = dtoRequest.DebitCurrency,
                        CreValDate = dtoRequest.CreditValueDate,
                        CreditAccountNumber = dtoRequest.CreditAccountNumber,
                        CreditCurrency = dtoRequest.CreditCurrency,
                        DebitTheirRef = dtoRequest.ReferenceNo,
                        CreditTheirRef = dtoRequest.ReferenceNo
                    }
                };

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                string rawRequestPayload = System.Text.Json.JsonSerializer.Serialize(request, options);

                using StringContent content = new(rawRequestPayload, Encoding.UTF8, "application/json");

                _logger.LogInformation("Initiating FundTransfer | UID: {uId}", uId);

                var client = _httpClientFactory.CreateClient("ExternalClient");
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.DefaultRequestHeaders.Add("companyId", companyId);

                if (!string.IsNullOrEmpty(uId))
                {
                    client.DefaultRequestHeaders.Add("uniqueIdentifier", uId);
                }

                HttpResponseMessage httpResponse = await client.PostAsync(_ftEndpoint, content);
                var rawResponsePayload = await httpResponse.Content.ReadAsStringAsync();

                var cleanedResult = JsonConvert.DeserializeObject(rawResponsePayload)?.ToString() ?? rawResponsePayload;
                ModelResponse.billpay? response = JsonConvert.DeserializeObject<ModelResponse.billpay>(cleanedResult);

                _logger.LogInformation("FundTransfer {Status} | UID: {uId} | Response: {response}", response?.Header?.Status, uId, rawResponsePayload);

                if (response?.Header?.Status?.Equals("SUCCESS", StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    dtoResponse = new ApiResponse.Response<DTOResponse.FundTransfer>
                    {
                        Success = true,
                        Message = "Transaction processed successfully.",
                        Data = new DTOResponse.FundTransfer
                        {
                            TransactionId = uId,
                            ReferenceNo = response.Header.Id,
                            Status = response.Header.Status.ToUpper(),
                            Amount = decimal.Parse(dtoRequest.Amount),
                            DebitCurrency = dtoRequest.DebitCurrency,
                            Creditcurrency = dtoRequest.CreditCurrency,
                            ProcessedAt = DateTime.Now
                        }
                    };
                }
                else
                {
                    dtoResponse = new ApiResponse.Response<DTOResponse.FundTransfer>
                    {
                        Success = false,
                        Message = "Transaction declined.",
                        Data = new DTOResponse.FundTransfer
                        {
                            TransactionId = uId,
                            ReferenceNo = response?.Header?.Id ?? string.Empty,
                            Status = "FAILED",
                            Amount = decimal.Parse(dtoRequest.Amount),
                            DebitCurrency = dtoRequest.DebitCurrency,
                            Creditcurrency = dtoRequest.CreditCurrency,
                            ProcessedAt = DateTime.Now
                        },
                        Errors =
                        [
                            new ApiResponse.Error
                            {
                                Code = response?.Error?.ErrorDetails?[0]?.Code ?? "T24-ERR",
                                Message = response?.Error?.ErrorDetails?[0]?.Message ?? "Transaction declined by Core Banking."
                            }
                        ]
                    };
                }
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "FundTransfer Socket Exception | UID: {uId}", uId);
                await _transactionLogService.LogExceptionAsync("TransactionService.FundTransfer.Timeout", ex);
                throw;
            }
            catch (System.Net.Sockets.SocketException ex) when (ex.ErrorCode == 995)
            {
                _logger.LogError(ex, "FundTransfer Socket Exception | UID: {uId}", uId);
                await _transactionLogService.LogExceptionAsync("TransactionService.FundTransfer.Socket", ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FundTransfer Unexpected Error | UID: {uId}", uId);
                await _transactionLogService.LogExceptionAsync("TransactionService.FundTransfer", ex);
                throw;
            }

            return dtoResponse;
        }

        public async Task<ApiResponse.Response<object>> Reversal(string companyId, string referenceNo)
        {
            ApiResponse.Response<object> dtoResponse = new();
            string targetUrl = $"{_reversalEndpoint}/{referenceNo}";

            _logger.LogInformation("Initiating Reversal | Ref: {Ref}", referenceNo);

            try
            {
                var client = _httpClientFactory.CreateClient("ExternalClient");
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.DefaultRequestHeaders.Add("companyId", companyId);
                client.DefaultRequestHeaders.Add("uniqueIdentifier", referenceNo);

                HttpResponseMessage httpResponse = await client.DeleteAsync(targetUrl);
                var rawResponsePayload = await httpResponse.Content.ReadAsStringAsync();

                var cleanedResult = JsonConvert.DeserializeObject(rawResponsePayload)?.ToString() ?? rawResponsePayload;
                ModelResponse.billpay? response = JsonConvert.DeserializeObject<ModelResponse.billpay>(cleanedResult);

                _logger.LogInformation("Reversal {Status} | Ref: {referenceNo} | Response: {response}", response?.Header?.Status, referenceNo, rawResponsePayload);

                if (response?.Header?.Status?.Equals("SUCCESS", StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    dtoResponse = new ApiResponse.Response<object>
                    {
                        Success = true,
                        Message = "Transaction reversed successfully."
                    };
                }
                else
                {
                    dtoResponse = new ApiResponse.Response<object>
                    {
                        Success = false,
                        Message = "Transaction reversal failed.",
                        Errors =
                        [
                            new ApiResponse.Error
                            {
                                Code = response?.Error?.ErrorDetails?[0]?.Code ?? "T24-ERR",
                                Message = response?.Error?.ErrorDetails?[0]?.Message ?? "Reversal declined."
                            }
                        ]
                    };
                }
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Reversal Socket Exception | Ref: {referenceNo}", referenceNo);
                await _transactionLogService.LogExceptionAsync("TransactionService.Reversal.Timeout", ex);
                throw;
            }
            catch (System.Net.Sockets.SocketException ex) when (ex.ErrorCode == 995)
            {
                _logger.LogError(ex, "Reversal Socket Exception | Ref: {referenceNo}", referenceNo);
                await _transactionLogService.LogExceptionAsync("TransactionService.Reversal.Socket", ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reversal Unexpected Error | Ref: {referenceNo}", referenceNo);
                await _transactionLogService.LogExceptionAsync("TransactionService.Reversal", ex);
                throw;
            }

            return dtoResponse;
        }

        public async Task<ApiResponse.Response<DTOResponse.TransactionStatus>> Status(string uId)
        {
            string script1 = File.ReadAllText(Path.Combine(_scriptPath, "transaction_status_v1.sql"));
            ApiResponse.Response<DTOResponse.TransactionStatus> dtoResponse = new();
            DateTime requestDate = DateTime.Now;
            string query = string.Format(script1, uId);
            string transactionStatus = "FAILED";
            string rawResponse = string.Empty;

            long logId = await _transactionLogService.LogRequestAsync(
                uId,
                "SQL:transaction_status_v1.sql",
                query,
                requestDate
            );

            _logger.LogInformation("Retrieving Status for UID: {UId}", uId);

            try
            {
                using SqlConnection con = new(_conString);
                using SqlCommand cmd = new(query, con);
                cmd.CommandType = CommandType.Text;

                await con.OpenAsync();
                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync() && !string.IsNullOrEmpty(reader["UID"].ToString()))
                {
                    dtoResponse = new ApiResponse.Response<DTOResponse.TransactionStatus>
                    {
                        Success = true,
                        Message = "Transaction retrieved.",
                        Data = new DTOResponse.TransactionStatus
                        {
                            UID = reader["UID"].ToString()!,
                            MessageKey = reader["MESSAGE_KEY"].ToString()!,
                            TransactionReference = reader["TRANS_REFERENCE"].ToString()!,
                            DateTimeReceived = reader["DATE_TIME_RECEIVED"].ToString()!,
                            DateTimeProcess = reader["DATE_TIME_PROCESS"].ToString()!,
                            Status = reader["STATUS"].ToString()!
                        }
                    };
                    transactionStatus = "SUCCESS";
                }
                else
                {
                    dtoResponse = new ApiResponse.Response<DTOResponse.TransactionStatus>
                    {
                        Success = false,
                        Message = $"The requested transaction (ID: {uId}) could not be found."
                    };
                    transactionStatus = "NOT_FOUND";
                    _logger.LogWarning("Failed to retrieve status for UID: {UId}", uId);
                }

                rawResponse = JsonConvert.SerializeObject(dtoResponse);
            }
            catch (Exception ex)
            {
                transactionStatus = "ERROR";
                rawResponse = ex.Message;
                _logger.LogError(ex, "Status Unexpected Error | UID: {uId}", uId);
                await _transactionLogService.LogExceptionAsync("TransactionService.Status", ex);
                throw;
            }
            finally
            {
                await _transactionLogService.LogResponseAsync(logId, rawResponse, DateTime.Now, transactionStatus);
            }

            return dtoResponse;
        }

        public async Task<ApiResponse.Response<object>> ClosingBalance(string referenceNo)
        {
            string script1 = File.ReadAllText(Path.Combine(_scriptPath, "closing_balance_v1.sql"));
            ApiResponse.Response<object> dtoResponse = new();
            DateTime requestDate = DateTime.Now;
            string query = string.Format(script1, referenceNo);
            string transactionStatus = "FAILED";
            string rawResponse = string.Empty;

            long logId = await _transactionLogService.LogRequestAsync(
                referenceNo,
                "SQL:closing_balance_v1.sql",
                query,
                requestDate
            );

            _logger.LogInformation("Retrieving closing balance | ReferenceNo: {referenceNo}", referenceNo);

            try
            {
                using SqlConnection con = new(_conString);
                using SqlCommand cmd = new(query, con);
                cmd.CommandType = CommandType.Text;

                await con.OpenAsync();
                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync() && !string.IsNullOrEmpty(reader["CLOSING_BALANCE"].ToString()))
                {
                    dtoResponse = new ApiResponse.Response<object>
                    {
                        Success = true,
                        Message = "Transaction retrieved.",
                        Data = new
                        {
                            ClosingBalance = reader["CLOSING_BALANCE"] != DBNull.Value
                                ? Convert.ToDecimal(reader["CLOSING_BALANCE"])
                                : 0m
                        }
                    };
                    transactionStatus = "SUCCESS";
                }
                else
                {
                    dtoResponse = new ApiResponse.Response<object>
                    {
                        Success = false,
                        Message = $"The requested transaction (Reference No: {referenceNo}) could not be found."
                    };
                    transactionStatus = "NOT_FOUND";
                    _logger.LogWarning("Failed to retrieve status for Reference No: {referenceNo}", referenceNo);
                }

                rawResponse = JsonConvert.SerializeObject(dtoResponse);
            }
            catch (Exception ex)
            {
                transactionStatus = "ERROR";
                rawResponse = ex.Message;
                _logger.LogError(ex, "ClosingBalance Unexpected Error | Reference No: {referenceNo}", referenceNo);
                await _transactionLogService.LogExceptionAsync("TransactionService.ClosingBalance", ex);
                throw;
            }
            finally
            {
                await _transactionLogService.LogResponseAsync(logId, rawResponse, DateTime.Now, transactionStatus);
            }

            return dtoResponse;
        }
    }
}