using Instapay.Api.Services.IService;
using Newtonsoft.Json;
using System.Text;
using Model = Instapay.Api.Models;
using Request = Instapay.Api.DTOs.Request;
using Response = Instapay.Api.DTOs.Response;


namespace Instapay.Api.Services
{
    public class TransactionService(IConfiguration configuration, ILogger<TransactionService> logger) : ITransactionService
    {
        private readonly ILogger<TransactionService> _logger = logger;
        private readonly string _ftEndpoint = configuration["Endpoints:FundTransfer"]!;
        private readonly string _authorization = $"{configuration["Config:Authorization"]!}";
        private readonly string _apiSecret = $"{configuration["Config:API_SECRET"]!}";
        private readonly string _audience = $"{configuration["Config:Audience"]!}";
        private readonly string _mallMerchantId = $"{configuration["Config:MallMerchantId"]!}";

        public async Task<Response.Transaction> FundTransfer(string uId, Request.Transaction dtoRequest)
        {

            Response.Transaction dtoResponse = new();

            try
            {
                string claims = $"{DateTime.Today:yyyyMMdd}{dtoRequest.TraceNo}";

                Model.Transaction.Request request = new()
                {
                    rqtag = _audience,
                    claims = claims,
                    srcacctname = dtoRequest.SenderName!.Length > 20 ? dtoRequest.SenderName[..20] : dtoRequest.SenderName,
                    srcacctnum = dtoRequest.SenderAccountNo!,
                    srcaccttype = dtoRequest.SenderAccountType,
                    srcBirthDt = dtoRequest.SenderBirthDate,
                    srcCityOfBirth = dtoRequest.SenderBirthPlace,
                    srcCtryOfBirth = dtoRequest.SenderBirthCountry,
                    destname = dtoRequest.ReceiverName!.Length > 30 ? dtoRequest.ReceiverName[..30] : dtoRequest.ReceiverName,
                    destbnkcode = dtoRequest.ReceiverBankCode,
                    destacctno = dtoRequest.ReceiverAccountNo,
                    destMobile = dtoRequest.ReceiverMobileNo,
                    tottxnamt = dtoRequest.Amount.ToString(),
                    merchantid = _mallMerchantId,
                };

                _logger.LogInformation("Initiating FundTransfer | uniqueIdentifier: {uId}  | Payload: {@request}", uId, System.Text.Json.JsonSerializer.Serialize(request));

                StringContent content = new(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                using HttpClient _client = new(handler)
                {
                    Timeout = TimeSpan.FromSeconds(60)

                };

                _client.DefaultRequestHeaders.TryAddWithoutValidation("ofi-api-key", _authorization);

                HttpResponseMessage httpResponse;
                httpResponse = await _client.PostAsync(_ftEndpoint, content);

                var result = "";
                result = httpResponse.Content.ReadAsStringAsync().Result;
                result = JsonConvert.DeserializeObject(httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult())!.ToString()!;

                Model.Transaction.Response response = JsonConvert.DeserializeObject<Model.Transaction.Response>(result)!;

                dtoResponse = new()
                {
                    Status = !response.trxst.Trim().ToUpper().Equals("RJCT", StringComparison.CurrentCultureIgnoreCase) ? "TRANSFER" : response.stcd.Trim().ToUpper().Equals("0000", StringComparison.CurrentCultureIgnoreCase) ? "STATUS" : "FAILED",
                    ReferenceNo = response.msgid.ToString(),
                    Code = response.trxst.Trim(),
                    Message = response.stmsg
                };

                _logger.LogInformation("FundTransfer {Status} | uniqueIdentifier: {uId} | Response: {response}", dtoResponse.Status, uId, result);

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

    }
}
