using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Text;
using System.Text.Json;
using Temenos.API.Services.IService.v1;
using ApiResponse = Common.DTOs.Response.Api;
using Model = Temenos.API.Models.v1;
using Request = Temenos.API.DTOs.Request.v1;
using Response = Temenos.API.DTOs.Response.v1;

namespace Temenos.API.Services.v1
{
    public class TransactionService(IConfiguration configuration, ILogger<TransactionService> logger) : ITransactionService
    {
        private readonly ILogger<TransactionService> _logger = logger;
        private readonly string _conString = configuration["ConnectionStrings:Temenos"]!;
        private readonly string _ftEndpoint = configuration["Endpoints:v1_FundTransfer"]!;
        private readonly string _reversalEndpoint = configuration["Endpoints:v1_Reversal"]!;
        private readonly string _scriptPath = Path.Combine($"{Directory.GetCurrentDirectory()}{"\\scripts"}");
        public async Task<ApiResponse.Response<Response.FundTransfer>> FundTransfer(string uId, string companyId, Request.FundTransfer dtoRequest)
        {
            ApiResponse.Response<Response.FundTransfer> dtoResponse = new();

            try
            {
                Model.Request.billpay request = new()
                {
                    Body = new Model.Request.billpayBody
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

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                StringContent? content = new(System.Text.Json.JsonSerializer.Serialize(request, options), Encoding.UTF8, "application/json");

                _logger.LogInformation("Initiating FundTransfer | uniqueIdentifier: {uId}  | Payload: {@request}", uId, System.Text.Json.JsonSerializer.Serialize(request, options));

                using HttpClient? _client = new();
                _client.Timeout = Timeout.InfiniteTimeSpan;
                _client.DefaultRequestHeaders.Add("companyId", companyId);

                if (!string.IsNullOrEmpty(uId))
                {
                    _client.DefaultRequestHeaders.Add("uniqueIdentifier", uId);
                }

                HttpResponseMessage httpResponse = await _client.PostAsync(_ftEndpoint, content);

                var result = httpResponse.Content.ReadAsStringAsync().Result;
                result = JsonConvert.DeserializeObject(httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult())!.ToString()!;

                Model.Response.billpay response = JsonConvert.DeserializeObject<Model.Response.billpay>(result)!;

                _logger.LogInformation("FundTransfer {Status} | uniqueIdentifier: {uId} | Response: {response}", response, uId, result);


                if (response.Header.Status.Equals("SUCCESS", StringComparison.CurrentCultureIgnoreCase))
                {
                    dtoResponse = new ApiResponse.Response<Response.FundTransfer>
                    {
                        Success = true,
                        Message = "Transaction processed successfully.",
                        Data = new Response.FundTransfer
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
                    dtoResponse = new ApiResponse.Response<Response.FundTransfer>
                    {
                        Success = false,
                        Message = "Transaction declined.",
                        Data = new Response.FundTransfer
                        {
                            TransactionId = uId,
                            ReferenceNo = response.Header.Id,
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
                                Code = response.Error.ErrorDetails[0].Code,
                                Message =  response.Error.ErrorDetails[0].Message
                            }
                        ]
                    };
                }

            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "FundTransfer Socket Exception | uniqueIdentifier: {UniqueIdentifier} | Response: {@Response}", uId, ex.Message);
                throw;
            }
            catch (System.Net.Sockets.SocketException ex) when (ex.ErrorCode == 995)
            {
                _logger.LogError(ex, "FundTransfer Socket Exception | uniqueIdentifier: {UniqueIdentifier} | Response: {@Response}", uId, ex.Message);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FundTransfer Unexpected Error | uniqueIdentifier: {UniqueIdentifier} | Response: {@Response}", uId, ex.Message);
                throw;
            }

            return dtoResponse;
        }
        public async Task<ApiResponse.Response<object>> Reversal(string companyId, string referenceNo)
        {
            ApiResponse.Response<object> dtoResponse = new();

            _logger.LogInformation("Initiating Reversal | ReferenceNo: {referenceNo}", referenceNo);

            try
            {

                using HttpClient _client = new();
                _client.Timeout = Timeout.InfiniteTimeSpan;
                _client.DefaultRequestHeaders.Add("companyId", companyId);
                HttpResponseMessage httpResponse = await _client.DeleteAsync($"{_reversalEndpoint}/{referenceNo}");

                var result = httpResponse.Content.ReadAsStringAsync().Result;
                result = JsonConvert.DeserializeObject(httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult())!.ToString()!;

                Model.Response.billpay response = JsonConvert.DeserializeObject<Model.Response.billpay>(result)!;

                _logger.LogInformation("Reversal {Status} | Reference No.: {ReferenceNo} | Response: {response}", response.Header.Status.ToUpper(), referenceNo, response);


                if (response.Header.Status.Equals("SUCCESS", StringComparison.CurrentCultureIgnoreCase))
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
                                Code = response.Error.ErrorDetails[0].Code,
                                Message =  response.Error.ErrorDetails[0].Message
                            }
                        ]
                    };
                }

            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Reversal Socket Exception | Reference No.: {ReferenceNo}| Response: {@Response}", referenceNo, ex.Message);
                throw;
            }
            catch (System.Net.Sockets.SocketException ex) when (ex.ErrorCode == 995)
            {
                _logger.LogError(ex, "Reversal Socket Exception | Reference No.: {ReferenceNo}| Response: {@Response}", referenceNo, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reversal Unexpected Error |  Reference No.: {ReferenceNo} | Response: {@Response}", referenceNo, ex.Message);
                throw;
            }

            return dtoResponse;
        }
        public async Task<ApiResponse.Response<Response.TransactionStatus>> Status(string uId)
        {
            string script1 = File.ReadAllText(Path.Combine(_scriptPath, "transaction_status_v1.sql"));

            ApiResponse.Response<Response.TransactionStatus> dtoResponse = new();

            _logger.LogInformation("Retrieving Status for UID: {UId}", uId);

            string query = string.Format(script1, uId);

            try
            {
                using SqlConnection con = new(_conString);
                using SqlCommand cmd = new(query, con);
                cmd.CommandType = CommandType.Text;

                await con.OpenAsync();

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                var statusResponse = new Response.TransactionStatus();

                if (await reader.ReadAsync())
                {

                    if (!string.IsNullOrEmpty(reader["UID"].ToString()!))
                    {
                        _logger.LogInformation("FT Status successfully retrieved for UID: {UId} | Response: {@Response}", uId, statusResponse);

                        dtoResponse = new ApiResponse.Response<Response.TransactionStatus>
                        {
                            Success = true,
                            Message = "Transaction retrieved.",
                            Data = new Response.TransactionStatus
                            {
                                UID = reader["UID"].ToString()!,
                                MessageKey = reader["MESSAGE_KEY"].ToString()!,
                                TransactionReference = reader["TRANS_REFERENCE"].ToString()!,
                                DateTimeReceived = reader["DATE_TIME_RECEIVED"].ToString()!,
                                DateTimeProcess = reader["DATE_TIME_PROCESS"].ToString()!,
                                Status = reader["STATUS"].ToString()!,
                                //MessageIn = reader["MSG_IN"].ToString()!,
                                //MessageOut = reader["MSG_OUT"].ToString()!.Split(',')[0]!
                            }
                        };

                    }
                   
                }
                else
                {
                    dtoResponse = new ApiResponse.Response<Response.TransactionStatus>
                    {
                        Success = false,
                        Message = $"The requested transaction (ID: {uId}) could not be found.",
                    };

                    _logger.LogWarning("Failed ro retrieved status for UID: {UId}", uId);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Status Unexpected Error | uniqueIdentifier: {UniqueIdentifier} | Response: {@Response}", uId, ex.Message);
                throw;
            }

            return dtoResponse;
        }
        public async Task<ApiResponse.Response<object>> ClosingBalance(string referenceNo)
        {
            string script1 = File.ReadAllText(Path.Combine(_scriptPath, "closing _balance_v1.sql"));
            
            ApiResponse.Response<object> dtoResponse = new();
            
            _logger.LogInformation("Retrieving closing balance | ReferenceNo: {referenceNo}", referenceNo);

            string query = string.Format(script1, referenceNo);

            try
            {
                using SqlConnection con = new(_conString);
                using SqlCommand cmd = new(query, con);
                cmd.CommandType = CommandType.Text;

                await con.OpenAsync();

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                var statusResponse = new Response.TransactionStatus();

                if (await reader.ReadAsync())
                {

                    if (!string.IsNullOrEmpty(reader["CLOSING_BALANCE"].ToString()!))
                    {
                        _logger.LogInformation("Closing balance retrieved for Reference No:: {referenceNo} | Response: {@statusResponse}", referenceNo, statusResponse);

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

                    }

                }
                else
                {
                    dtoResponse = new ApiResponse.Response<object>
                    {
                        Success = false,
                        Message = $"The requested transaction (Reference No: {referenceNo}) could not be found.",
                    };

                    _logger.LogWarning("Failed ro retrieved status for Reference No: {referenceNo}", referenceNo);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Status Unexpected Error | Reference No: {referenceNo} | Response: {@Response}", referenceNo, ex.Message);
                throw;
            }

            return dtoResponse;
        }

    }
}
