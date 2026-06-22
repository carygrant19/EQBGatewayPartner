using Common.DTOs.Response.Api;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Security.Cryptography;
using Temenos.API.Services.IService.v1;
using ApiResponse = Common.DTOs.Response.Api;
using Response = Temenos.API.DTOs.Response.v1;
using Model = Temenos.API.Models.v1;

namespace Temenos.API.Services.v1
{
    public class AccountService(IConfiguration configuration, ILogger<TransactionService> logger) : IAccountService
    {
        private readonly ILogger<TransactionService> _logger = logger;
        private readonly string _conString = configuration["ConnectionStrings:Temenos"]!;
        private readonly string _balanceInquiry = configuration["Endpoints:v1_BalanceInquiry"]!;
        private readonly string _scriptPath = Path.Combine($"{Directory.GetCurrentDirectory()}{"\\scripts"}");

        public async Task<ApiResponse.Response<Response.AccountDetails>> Details(string accountNo)
        {
            string script1 = File.ReadAllText(Path.Combine(_scriptPath, "account_details_v1.sql"));

            ApiResponse.Response<Response.AccountDetails> dtoResponse = new();

            _logger.LogInformation("Retrieving account details for AccountNo: {accountNo}", accountNo);

            string query = string.Format(script1, accountNo);

            try
            {
                using SqlConnection con = new(_conString);
                using SqlCommand cmd = new(query, con);
                cmd.CommandType = CommandType.Text;

                await con.OpenAsync();

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                var statusResponse = new Response.AccountDetails();

                if (await reader.ReadAsync())
                {

                    if (!string.IsNullOrEmpty(reader["ACCOUNT_NUMBER"].ToString()!))
                    {
                        _logger.LogInformation("Account details successfully retrieved for AccountNo: {accountNo}", accountNo);

                        dtoResponse = new ApiResponse.Response<Response.AccountDetails>
                        {
                            Success = true,
                            Message = "Account details retrieved.",
                            Data = new Response.AccountDetails
                            {
                                AccountNo = reader["ACCOUNT_NUMBER"].ToString()!,
                                Description = reader["DESCRIPTION"].ToString()!,
                                BirthDate = string.IsNullOrWhiteSpace(reader["DATE_OF_BIRTH"].ToString()!) ? null : reader["DATE_OF_BIRTH"].ToString()!,
                                BirthPlace = string.IsNullOrWhiteSpace(reader["PLACE_OF_BIRTH"].ToString()!) ? null : reader["PLACE_OF_BIRTH"].ToString()!,
                                InCorpDate = string.IsNullOrWhiteSpace(reader["BIRTH_INCORP_DATE"].ToString()!) ? null : reader["BIRTH_INCORP_DATE"].ToString()!,
                                InCorpPlace = string.IsNullOrWhiteSpace(reader["PLACE_OF_INCORP"].ToString()!) ? null : reader["PLACE_OF_INCORP"].ToString()!,
                                Nationality = reader["PLACE_OF_INCORP"].ToString()!,
                                Phone = reader["PHONE_1"].ToString()!,
                                Sms = reader["SMS_1"].ToString()!,
                            }
                        };

                    }

                }
                else
                {
                    dtoResponse = new ApiResponse.Response<Response.AccountDetails>
                    {
                        Success = false,
                        Message = $"The requested account details (Account No: {accountNo}) could not be found.",
                    };

                    _logger.LogWarning("Failed ro retrieved account details for Account No: {accountNo}", accountNo);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Account details unexpected Error | Account No: {accountNo} | Response: {@Response}", accountNo, ex.Message);
                throw;
            }

            return dtoResponse;
        }

        public async Task<ApiResponse.Response<Response.BalanceInquiry>> InquireBalance(string accountNo)
        {
            ApiResponse.Response<Response.BalanceInquiry> dtoResponse = new();

            _logger.LogInformation("Retrieving balance inquiry for AccountNo: {accountNo}", accountNo);

            try
            {
                using HttpClient? _client = new();
                _client.Timeout = Timeout.InfiniteTimeSpan;
     
                HttpResponseMessage httpResponse = await _client.GetAsync($"{_balanceInquiry}/{accountNo}");

                var result = httpResponse.Content.ReadAsStringAsync().Result;
                result = JsonConvert.DeserializeObject(httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult())!.ToString()!;

                Model.Response.balanceInquiry response = JsonConvert.DeserializeObject<Model.Response.balanceInquiry>(result)!;


                _logger.LogInformation("Balance Inquiry {Status} | Account No: {accountNo} | Response: {response}", response, accountNo, result);

                if (response.Error == null)
                {
                    dtoResponse = new ApiResponse.Response<Response.BalanceInquiry>
                    {
                        Success = true,
                        Message = "Balance details retrieved.",
                        Data = new Response.BalanceInquiry
                        {
                            AccountNo = accountNo,
                            Balance = response.Body[0].Balance.ToString(),
                            AccountName = response.Body[0].AccountName.ToString(),
                            AccountType = response.Body[0].AccountType.ToString(),
                            Currency = response.Body[0].Currency.ToString(),
                            Classification = response.Body[0].Classification.ToString(),
                        }
                    };
                }
                else
                {
                    dtoResponse = new ApiResponse.Response<Response.BalanceInquiry>
                    {
                        Success = false,
                        Message = "Failed to retrieve details.",
                        Errors =
                        [
                            new ApiResponse.Error
                            {
                                Code = response.Error.Code.ToString(),
                                Message =  response.Error.Message.ToString()
                            }
                        ]
                    };
                }

            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Balance Inquiry Socket Exception | Account No: {UniqueIdentifier} | Response: {@Response}", accountNo, ex.Message);
                throw;
            }
            catch (System.Net.Sockets.SocketException ex) when (ex.ErrorCode == 995)
            {
                _logger.LogError(ex, "Balance Inquiry  Socket Exception | Account No: {UniqueIdentifier} | Response: {@Response}", accountNo, ex.Message);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Balance Inquiry  Unexpected Error | Account No: {UniqueIdentifier} | Response: {@Response}", accountNo, ex.Message);
                throw;
            }

            return dtoResponse;
        }
    }
}
