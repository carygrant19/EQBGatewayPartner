using Newtonsoft.Json;
using Pesonet.API.Services.IService;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Request = Pesonet.API.DTOs.Request;
using Response = Pesonet.API.DTOs.Response;
using Status = Pesonet.API.Models.Status;
using Model = Pesonet.API.Models;

namespace Pesonet.API.Services
{
    public class TransactionService(IConfiguration configuration, ILogger<TransactionService> logger) : ITransactionService
    {

        private readonly ILogger<TransactionService> _logger = logger;
        private readonly string _ftEndpoint = configuration["Endpoints:FundTransfer"]!;
        private readonly string _statusEndpoint = configuration["Endpoints:Status"]!;
        private readonly string _certFilePath = Path.Combine(AppContext.BaseDirectory, $"{configuration["Config:CertFilePath"]!}");
        private readonly string _certPassword = $"{configuration["Config:CertPassword"]!}";
        private readonly string _apiKey = $"{configuration["Config:API_KEY"]!}";
        private readonly string _apiSecret = $"{configuration["Config:API_SECRET"]!}";

        public async Task<Response.Transaction> FundTransfer(string uId, Request.Transaction dtoRequest)
        {
            Response.Transaction dtoResponse = new();

            try
            {
                Model.Transaction.Request request = new();

                Model.Transaction.FIToFICstmrCdtTrf fIToFICstmrCdtTrf = new()
                {
                    GrpHdr = new()
                    {
                        MsgId = null,
                        CreDtTm = null,
                        NbOfTxs = 1,
                        TtlIntrBkSttlmAmt = new() { Ccy = "PHP", value = dtoRequest.Amount.ToString() },
                        IntrBkSttlmDt = null,
                        SttlmInf = new() { SttlmMtd = "CLRG" },
                        InstgAgt = new() { FinInstnId = new() { BICFI = "EQSNPHM1XXX" } },
                        InstdAgt = new() { FinInstnId = new() { BICFI = "PCHCPHM1XXX" } },
                        PmtTpInf = new() { LclInstrm = new() { Prtry = DateTime.Now.ToString("yyyyMMddHHmmss") } }
                    },
                    CdtTrfTxInf = []
                };

                Model.Transaction.CdtTrfTxInf cdtTrfTxInf = new()
                {
                    PmtId = new() { EndToEndId = null, TxId = DateTime.Now.ToString("yyyyMMddHHmmss") },
                    PmtTpInf = new() { SvcLvl = new() { Prtry = "NURG" }, CtgyPurp = new() { Cd = "CASH" } },
                    IntrBkSttlmAmt = new() { Ccy = "PHP", value = dtoRequest.Amount.ToString() },
                    ChrgBr = "SLEV",
                    Dbtr = new() { Nm = dtoRequest.SenderName!, PstlAdr = ["PH"] },
                    DbtrAcct = new() { Id = new() { Othr = new() { Id = dtoRequest.SenderAccountNo! } } },
                    DbtrAgt = new() { FinInstnId = new() { BICFI = "EQSNPHM1XXX" } },
                    Cdtr = new() { Nm = dtoRequest.ReceiverName!, PstlAdr = [dtoRequest.ReceiverAddress!] },
                    CdtrAcct = new() { Id = new() { Othr = new() { Id = dtoRequest.ReceiverAccountNo } } },
                    CdtrAgt = new() { FinInstnId = new() { BICFI = dtoRequest.Bicfi } },
                    RmtInf = new()
                    {
                        Ustrd = new() { rfi_reference_number = null, ofi_customer_reference_number = null, rfi_customer_reference_number = null }
                    }
                };

                fIToFICstmrCdtTrf.CdtTrfTxInf.Add(cdtTrfTxInf);
                request.FIToFICstmrCdtTrf = fIToFICstmrCdtTrf;

                StringContent content = new(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                _logger.LogInformation("Initiating FundTransfer | uniqueIdentifier: {uId}  | Payload: {@request}", uId, System.Text.Json.JsonSerializer.Serialize(request));

                using HttpClientHandler handler = new()
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                };

               X509Certificate2Collection certificates = X509CertificateLoader.LoadPkcs12CollectionFromFile(
                    _certFilePath,
                    _certPassword,
                    X509KeyStorageFlags.DefaultKeySet | X509KeyStorageFlags.PersistKeySet
                );
               handler.ClientCertificates.AddRange(certificates);

                //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                //ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                //ServicePointManager.DefaultConnectionLimit = 9999;

                using HttpClient client = new(handler);
                client.Timeout = Timeout.InfiniteTimeSpan;


                client.BaseAddress = new Uri(_ftEndpoint);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
                client.DefaultRequestHeaders.TryAddWithoutValidation("api_key", _apiKey);
                if (string.IsNullOrWhiteSpace(request.FIToFICstmrCdtTrf.GrpHdr.TtlIntrBkSttlmAmt.value))
                    client.DefaultRequestHeaders.TryAddWithoutValidation("signature", GenerateSignature(null, _apiKey, _apiSecret));
                else
                    client.DefaultRequestHeaders.TryAddWithoutValidation("signature", GenerateSignature(request.FIToFICstmrCdtTrf.GrpHdr.TtlIntrBkSttlmAmt.value, _apiKey, _apiSecret));

                HttpResponseMessage httpResponse = await client.PostAsync(_ftEndpoint, content);

                var result = JsonConvert.DeserializeObject(httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult())!.ToString()!;

                Model.Transaction.Response response = JsonConvert.DeserializeObject<Model.Transaction.Response>(result)!;

                dtoResponse = new()
                {
                    Status = response.outward_message.status.Trim().ToUpper().Equals("PROCESSED", StringComparison.CurrentCultureIgnoreCase) ? "SUCCESS" : "FAILED",
                    ReferenceNo = response.outward_message.status.Trim().ToUpper().Equals("PROCESSED", StringComparison.CurrentCultureIgnoreCase) ? response.outward_message.seq.ToString() : "0",
                    Code = null,
                    Message = !response.outward_message.status.Trim().ToUpper().Equals("PROCESSED", StringComparison.CurrentCultureIgnoreCase) ? response.error.message : null
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

        public async Task<Response.Status?> Status(string seqNo) 
        {

            var dtoResponse = new Response.Status();

            try
            {

                string hostEndpoint = $"{_statusEndpoint}/{seqNo}";
                HttpResponseMessage response;

                using HttpClientHandler handler = new()
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                    ClientCertificateOptions = ClientCertificateOption.Manual
                };

                X509Certificate2Collection certificates = X509CertificateLoader.LoadPkcs12CollectionFromFile(
                          _certFilePath,
                          _certPassword,
                          X509KeyStorageFlags.DefaultKeySet | X509KeyStorageFlags.PersistKeySet
                      );
                handler.ClientCertificates.AddRange(certificates);
                
                _logger.LogInformation("Status | uniqueIdentifier: {seqNo}", seqNo);

                using HttpClient client = new(handler);
                client.Timeout = Timeout.InfiniteTimeSpan;

                client.BaseAddress = new Uri(hostEndpoint);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
                client.DefaultRequestHeaders.TryAddWithoutValidation("api_key", _apiKey);
                client.DefaultRequestHeaders.TryAddWithoutValidation("signature", GenerateSignature("", _apiKey, _apiSecret));

                response = await client.GetAsync(hostEndpoint);
                var result = JsonConvert.DeserializeObject<Status.Response>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult())!;

                dtoResponse = new Response.Status
                {
                    No = int.Parse(seqNo),
                    Stts = string.IsNullOrEmpty(result!.TxInfAndSts[0].TxSts) ? "0000" : result!.TxInfAndSts[0].TxSts,
                    Name = string.IsNullOrEmpty(result.TxInfAndSts[0].OrgnlTxRef.Cdtr.Nm) ? "" : result.TxInfAndSts[0].OrgnlTxRef.Cdtr.Nm,
                    Acct_no = string.IsNullOrEmpty(result.TxInfAndSts[0].OrgnlTxRef.CdtrAcct.Id.Othr.Id) ? "" : result.TxInfAndSts[0].OrgnlTxRef.Cdtr.Nm,
                    Ccy = string.IsNullOrEmpty(result.TxInfAndSts[0].OrgnlTxRef.Amt.EqvtAmt.CcyOfTrf) ? "" : result.TxInfAndSts[0].OrgnlTxRef.Amt.EqvtAmt.CcyOfTrf,
                    Amt = decimal.Parse(string.IsNullOrEmpty(result.TxInfAndSts[0].OrgnlTxRef.Amt.EqvtAmt.Amt) ? "0" : result.TxInfAndSts[0].OrgnlTxRef.Amt.EqvtAmt.Amt),
                    Bnk = string.IsNullOrEmpty(result.TxInfAndSts[0].OrgnlTxRef.CdtrAgt.FinInstnId.BICFI) ? "" : result.TxInfAndSts[0].OrgnlTxRef.CdtrAgt.FinInstnId.BICFI,
                    DtTmCrdt = GetDate(result.TxInfAndSts[0].AccptncDtTm.ToString()!),
                    Rslt_Msg = TransalateStatus(result!.TxInfAndSts[0].TxSts)
                };

                _logger.LogInformation("Status {Status} | uniqueIdentifier: {seqNo} | Response: {response}", dtoResponse.Rslt_Msg, seqNo, result);
            }
            catch (TaskCanceledException ex)
            {
                dtoResponse = null;

                _logger.LogError(ex, "Status Socket Exception | uniqueIdentifier: {UniqueIdentifier} | Response: {@Response}", seqNo, dtoResponse);
            }
            catch (System.Net.Sockets.SocketException ex) when (ex.ErrorCode == 995)
            {

                dtoResponse = null;

                _logger.LogError(ex, "FundTransfer Socket Exception | uniqueIdentifier: {UniqueIdentifier} |  Response: {@Response}", seqNo, dtoResponse);
            }
            catch (Exception ex)
            {

                dtoResponse = null;

                _logger.LogError(ex, "FundTransfer Unexpected Error | uniqueIdentifier: {UniqueIdentifier} |  Response: {@Response}", seqNo, dtoResponse);

            }


            return dtoResponse;
        }

        #region private

        private static string GenerateSignature(string? amountOrId, string apiKey, string apiSecret)
        {
            string Signature;
            string ToBeEncrypted;

            if (string.IsNullOrEmpty(amountOrId))
                ToBeEncrypted = apiKey + apiSecret;
            else
                ToBeEncrypted = apiKey + apiSecret + amountOrId;

            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ToBeEncrypted));
            StringBuilder sb = new();
            for (int i = 0; i < bytes.Length; i++)
                sb.Append(bytes[i].ToString("x2"));
            Signature = sb.ToString();
            return Signature;
        }

        private static string TranslateResponse(HttpResponseMessage response)
        {
            if (response.ReasonPhrase == "OK")
            {
                return JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result)!.ToString()!;
            }
            else
            {
                return $"{{ \"code\": \"{(int)response.StatusCode}\", status : \"FAILED\", message : \"{response.ReasonPhrase}\" }}";
            }
        }

        private static string TransalateStatus(string? transactionStatus)
        {
            string status = transactionStatus switch
            {
                "DS07" => "Successfully Processed",
                "AC03" => "Invalid creditor Account Number",
                "AC06" => "Blocked Account Number",
                "BE01" => "Inconsistent With End Customer",
                "CURR" => "Incorrect Currency",
                "AM21" => "Limit Exceeded",
                "DS02" => "Order Cancelled",
                "DS04" => "Order Rejected",
                "DS06" => "Transfer Order",
                "AB07" => "Receiver Offline",
                "RR04" => "Regulatory Reason",
                _ => "Not Yet Processed",
            };

            return status;
        }
        private static string GetDate(string? date)
        {
            string formattedString = "";

            if (!string.IsNullOrWhiteSpace(date))
            {
                long unixTimestamp = long.Parse(date[..10]);
                DateTime formattedDate = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;

                formattedString = formattedDate.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine(formattedString);
            }
            return formattedString;
        }

        #endregion

    }
}
