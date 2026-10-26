using Common.Services.IService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Temenos.API.Services.IService.v1.loans;
using ApiResponse = Common.DTOs.Response.Api;
using DTOResponse = Temenos.API.DTOs.Response.v1.loans;
using ModelResponse = Temenos.API.Models.v1.Response.loans;

namespace Temenos.API.Services.v1.loans
{
    public class AccountService(
        IConfiguration configuration,
        ILogger<AccountService> logger,
        IHttpClientFactory httpClientFactory,
        ITransactionLogService transactionLogService) : IAccountService
    {
        private readonly ILogger<AccountService> _logger = logger;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ITransactionLogService _transactionLogService = transactionLogService;
        private readonly string _conString = configuration["ConnectionStrings:Temenos"]!;
        private readonly string _amortizationSchedule = configuration["Endpoints:v1_AmortizationSchedule"]!;
        private readonly string _scriptPath = Path.Combine($"{Directory.GetCurrentDirectory()}\\scripts");

        public async Task<ApiResponse.Response<List<DTOResponse.AmortizationSchedule>>> AmortizationSchedule(string arrangementId)
        {
            string pageStart = "1";
            string pageSize = "999";
            ApiResponse.Response<List<DTOResponse.AmortizationSchedule>> dtoResponse = new();

            _logger.LogInformation("Retrieving amortization schedule for ArrangementId: {arrangementId}", arrangementId);

            try
            {
                var client = _httpClientFactory.CreateClient("ExternalClient");
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.DefaultRequestHeaders.Add("uniqueIdentifier", arrangementId);

                string targetUrl = $"{_amortizationSchedule}?page_start={pageStart}&arrangementId={arrangementId}&page_size={pageSize}";
                HttpResponseMessage httpResponse = await client.GetAsync(targetUrl);

                var result = await httpResponse.Content.ReadAsStringAsync();
                var cleanedResult = JsonConvert.DeserializeObject(result)?.ToString() ?? result;

                ModelResponse.amortizationSchedule? response = JsonConvert.DeserializeObject<ModelResponse.amortizationSchedule>(cleanedResult);

                _logger.LogInformation("Amortization Schedule {Status} | Arrangement Id {arrangementId} | Response: {response}", response?.Error == null ? "SUCCESS" : "FAILED", arrangementId, result);

                if (response?.Error == null && response?.Body != null)
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
                                Code = response?.Error?.Code?.ToString() ?? "T24-ERR",
                                Message = response?.Error?.Message?.ToString() ?? "Unable to retrieve amortization schedule."
                            }
                        ]
                    };
                }
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Amortization Schedule Socket Exception | Arrangement Id: {arrangementId} | Response: {Response}", arrangementId, ex.Message);
                await _transactionLogService.LogExceptionAsync("Loans.AccountService.AmortizationSchedule.Timeout", ex);
                throw;
            }
            catch (System.Net.Sockets.SocketException ex) when (ex.ErrorCode == 995)
            {
                _logger.LogError(ex, "Amortization Schedule Socket Exception | Arrangement Id: {arrangementId} | Response: {Response}", arrangementId, ex.Message);
                await _transactionLogService.LogExceptionAsync("Loans.AccountService.AmortizationSchedule.Socket", ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Amortization Schedule Unexpected Error | Arrangement Id: {arrangementId} | Response: {Response}", arrangementId, ex.Message);
                await _transactionLogService.LogExceptionAsync("Loans.AccountService.AmortizationSchedule", ex);
                throw;
            }

            return dtoResponse;
        }
    }
}