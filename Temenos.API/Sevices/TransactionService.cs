using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;

using System.Text;
using Temenos.API.Models;
using Temenos.API.Sevices.IService;
using Model = Temenos.API.Models;
using Request = Temenos.API.DTOs.Request;
using Response = Temenos.API.DTOs.Response;

namespace Temenos.API.Sevices
{
    public class TransactionService(IConfiguration configuration, ILogger<TransactionService> logger) : ITransactionService
    {
        private readonly ILogger<TransactionService> _logger = logger;
        //private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly string _conString = configuration["ConnectionStrings:Temenos"]!;
        private readonly string _ftEndpoint = configuration["Endpoints:FundTransfer"]!;
        private readonly string _reversalEndpoint = configuration["Endpoints:Reversal"]!;
        public async Task<Response.Transaction> FundTransfer(string uId, string companyId, Request.Transaction dtoRequest)
        {
            Response.Transaction dtoResponse = new();

           

            try
            {
                TransactionRequest request = new()
                {
                    Body = new TransactionRequestBody
                    {
                        TransactionType = dtoRequest.TransactionType,
                        DebValDate = dtoRequest.DebitValueDate,
                        DebitAmount = dtoRequest.DebitAmount,
                        DebitAccountNumber = dtoRequest.DebitAccountNumber,
                        DebitCurrency = dtoRequest.DebitCurrency,
                        CreValDate = dtoRequest.CreditValueDate,
                        CreditAccountNumber = dtoRequest.CreditAccountNumber,
                        CreditCurrency = dtoRequest.CreditCurrency,
                        OrderingCust = dtoRequest.OrderingCust,
                        OrderingBank = dtoRequest.OrderingBank,
                        DebitTheirRef = dtoRequest.ReferenceNo,
                        CreditTheirRef = dtoRequest.ReferenceNo
                    }
                };

                _logger.LogInformation("Initiating FundTransfer | uniqueIdentifier: {uId}  | Payload: {@request}", uId, request);

                StringContent? content = new(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

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

                TransactionResponse response = JsonConvert.DeserializeObject<Model.TransactionResponse>(result)!;

                dtoResponse = new()
                {
                    Status = response.Header.Status.ToUpper(),
                    ReferenceNo = response.Header.Status.ToUpper() == "SUCCESS" ? response.Header.Id : null,
                    Code = response.Header.Status.ToUpper() != "SUCCESS" ? response.Error.ErrorDetails[0].Code : null,
                    Message = response.Header.Status.ToUpper() != "SUCCESS" ? response.Error.ErrorDetails[0].Code : null
                };

                _logger.LogInformation("FundTransfer {Status} | uniqueIdentifier: {uId} | Response: {@response}", dtoResponse.Status, uId, response);

            }
            catch (TaskCanceledException ex)
            {
                dtoResponse = new()
                {
                    Status = "ERROR",
                    Message = ex.Message
                };

                _logger.LogError(ex, "FundTransfer Socket Exception | uniqueIdentifier: {UniqueIdentifier} | Response: {@Request}", uId, dtoResponse);
            }
            catch (System.Net.Sockets.SocketException ex) when (ex.ErrorCode == 995)
            {

                dtoResponse = new()
                {
                    Status = "ERROR",
                    Message = ex.Message
                };

                _logger.LogError(ex, "FundTransfer Socket Exception | uniqueIdentifier: {UniqueIdentifier} | Response: {@Request}", uId, dtoResponse);
            }
            catch (Exception ex)
            {

                dtoResponse = new()
                {
                    Status = "ERROR",
                    Message = ex.Message
                };

                _logger.LogError(ex, "FundTransfer Unexpected Error | uniqueIdentifier: {UniqueIdentifier} | Response: {@Request}", uId, dtoResponse);

            }

            return dtoResponse;
        }
        public async Task<Response.Transaction> Reversal(string companyId, string referenceNo)
        {
            Response.Transaction dtoResponse = new();

            _logger.LogInformation("Initiating Reversal | ReferenceNo: {referenceNo}", referenceNo);
            try
            {

                using HttpClient _client = new();
                _client.Timeout = Timeout.InfiniteTimeSpan;
                _client.DefaultRequestHeaders.Add("companyId", companyId);
                HttpResponseMessage httpResponse = await _client.DeleteAsync($"{_reversalEndpoint}/{referenceNo}");

                var result = httpResponse.Content.ReadAsStringAsync().Result;
                result = JsonConvert.DeserializeObject(httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult())!.ToString()!;

                TransactionResponse response = JsonConvert.DeserializeObject<Model.TransactionResponse>(result)!;

                _logger.LogInformation("Reversal {Status} | Reference No.: {ReferenceNo} | Response: {@response}", dtoResponse.Status, referenceNo, response);

            }
            catch (TaskCanceledException ex)
            {
                dtoResponse = new()
                {
                    Status = "ERROR",
                    Message = ex.Message
                };

                _logger.LogError(ex, "Reversal Socket Exception | Reference No.: {ReferenceNo}| Response: {@Request}", referenceNo, dtoResponse);
            }
            catch (System.Net.Sockets.SocketException ex) when (ex.ErrorCode == 995)
            {

                dtoResponse = new()
                {
                    Status = "ERROR",
                    Message = ex.Message
                };

                _logger.LogError(ex, "Reversal Socket Exception | Reference No.: {ReferenceNo}| Response: {@Request}", referenceNo, dtoResponse);
            }
            catch (Exception ex)
            {

                dtoResponse = new()
                {
                    Status = "ERROR",
                    Message = ex.Message
                };

                _logger.LogError(ex, "Reversal Unexpected Error |  Reference No.: {ReferenceNo} | Response: {@Request}", referenceNo, dtoResponse);

            }

            return dtoResponse;
        }
        public async Task<Response.TransactionStatus?> Status(string uId)
        {
            _logger.LogInformation("Querying FT Status for UID: {UId}", uId);

            string query = @"SELECT RECID AS UID, MESSAGE_KEY, TRANS_REFERENCE, DATE_TIME_RECD AS DATE_TIME_RECEIVED, 
                            DATE_TIME_PROC AS DATE_TIME_PROCESS, STATUS, MSG_IN, MSG_OUT 
                     FROM V_F_OFS_REQUEST_DETAIL 
                     WHERE RECID = @uId";
            try
            {
                using SqlConnection con = new(_conString);
                using SqlCommand cmd = new(query, con);
                cmd.CommandType = CommandType.Text;

                cmd.Parameters.AddWithValue("@uId", uId.ToUpper());

                await con.OpenAsync();

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var statusResponse = new Response.TransactionStatus()
                    {
                        UID = reader["UID"].ToString()!,
                        MESSAGE_KEY = reader["MESSAGE_KEY"].ToString()!,
                        TRANS_REFERENCE = reader["TRANS_REFERENCE"].ToString()!,
                        DATE_TIME_RECEIVED = reader["DATE_TIME_RECEIVED"].ToString()!,
                        DATE_TIME_PROCESS = reader["DATE_TIME_PROCESS"].ToString()!,
                        STATUS = reader["STATUS"].ToString()!,
                        MSG_IN = reader["MSG_IN"].ToString()!,
                        MSG_OUT = reader["MSG_OUT"].ToString()!.Split(',')[0]!
                    };

                    _logger.LogInformation("FT Status successfully retrieved for UID: {UId} | Response: {@Response}", uId, statusResponse);

                    return statusResponse;
                }

                _logger.LogWarning("No FT Status record found in database for UID: {UId}", uId);

                return null;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error occurred while querying FT Status for UID: {UId}", uId);
                throw;
            }

        }
    }
}
