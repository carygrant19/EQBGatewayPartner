using Newtonsoft.Json;
using System.Data;
using ApiResponse = Common.DTOs.Response.Api;
using DTOResponse = Temenos.API.DTOs.Response.v1.loans;
using ModelResponse = Temenos.API.Models.v1.Response.loans;
using Temenos.API.Services.IService.v1.loans;

namespace Temenos.API.Services.v1.loans
{
    public class AccountService(IConfiguration configuration, ILogger<AccountService> logger) : IAccountService
    {
        private readonly ILogger<AccountService> _logger = logger;
        private readonly string _conString = configuration["ConnectionStrings:Temenos"]!;
        private readonly string _amortizationSchedule = configuration["Endpoints:v1_AmortizationSchedule"]!;
        private readonly string _scriptPath = Path.Combine($"{Directory.GetCurrentDirectory()}{"\\scripts"}");


        public async Task<ApiResponse.Response<List<DTOResponse.AmortizationSchedule>>> AmortizationSchedule(string arrangementId)
        {
            string pageStart = "1";
            string pageSize = "999";
            ApiResponse.Response<List<DTOResponse.AmortizationSchedule>> dtoResponse = new();

            _logger.LogInformation("Retrieving balance inquiry for AccountNo: {accountNo}", arrangementId);

            try
            {
                using HttpClient? _client = new();
                _client.Timeout = Timeout.InfiniteTimeSpan;

                HttpResponseMessage httpResponse = await _client.GetAsync($"{_amortizationSchedule}?page_start={pageStart}&arrangementId={arrangementId}&page_size={pageSize}");

                var result = httpResponse.Content.ReadAsStringAsync().Result;
                result = JsonConvert.DeserializeObject(httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult())!.ToString()!;

                ModelResponse.amortizationSchedule response = JsonConvert.DeserializeObject<ModelResponse.amortizationSchedule>(result)!;


                _logger.LogInformation("Amortization Schedule {Status} | Arrangement Id {arrangementId} | Response: {response}", response, arrangementId, result);

                if (response.Error == null)
                {
                    dtoResponse = new ApiResponse.Response<List<DTOResponse.AmortizationSchedule>>
                    {
                        Success = true,
                        Message = "Amortization Schedule retrieved.",
                        Data = response.Body.Select(item => new DTOResponse.AmortizationSchedule
                        {
                            DueDate = item.dueDate ?? "",
                            TotalDue = item.totalDue ?? "",
                            Principal = item.principal ?? "",
                            Interest = item.interest ?? "",
                            Outstanding = item.outstanding ?? ""
                        }).ToList()
                    };
                }
                else
                {
                    dtoResponse = new ApiResponse.Response<List<DTOResponse.AmortizationSchedule>>
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
                _logger.LogError(ex, "Balance Inquiry Socket Exception | Arrangement Id: {UniqueIdentifier} | Response: {@Response}", arrangementId, ex.Message);
                throw;
            }
            catch (System.Net.Sockets.SocketException ex) when (ex.ErrorCode == 995)
            {
                _logger.LogError(ex, "Balance Inquiry  Socket Exception | Arrangement Id: {UniqueIdentifier} | Response: {@Response}", arrangementId, ex.Message);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Balance Inquiry  Unexpected Error | Arrangement Id: {UniqueIdentifier} | Response: {@Response}", arrangementId, ex.Message);
                throw;
            }

            return dtoResponse;
        }


    }
}
