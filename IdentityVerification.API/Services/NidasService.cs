using IdentityVerification.API.Models.Response.Nidas;
using IdentityVerification.API.Services.IService;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Request = IdentityVerification.API.DTO.Request;
using RequestModel = IdentityVerification.API.Models.Request.Nidas;
using Response = IdentityVerification.API.DTO.Response;
using ResponseModel = IdentityVerification.API.Models.Response.Nidas;
using ApiResponse = Common.DTOs.Response.Api;

namespace IdentityVerification.API.Services
{
    public class NidasService(IConfiguration configuration, ILogger<NidasService> logger) : INidasService
    {
        private readonly ILogger<NidasService> _logger = logger;
        private readonly string _Host = $"{configuration["Nidas:Host"]!}";
        private readonly string _ClientID = $"{configuration["Nidas:ClientID"]!}";
        private readonly string _ClientSecret = $"{configuration["Nidas:ClientSecret"]!}";
        private readonly string _Authentication = $"{configuration["Nidas:Endpoint:Authentication"]!}";


        public async Task<object> Verify(Request.Nidas.Verify dtoRequest)
        {

            //Response.Transaction dtoResponse = new();

            //try
            //{

                var authResponse = Authentication().Result;



            if (authResponse != null)
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                using HttpClient _client = new(handler) { Timeout = TimeSpan.FromSeconds(60) };

                _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {authResponse.Data.AccessToken}");

                RequestModel.Verify request = dtoRequest;

                _logger.LogInformation("Initiating Verification | Payload: {@request}", System.Text.Json.JsonSerializer.Serialize(request));

                var httpResponse = await _client.PostAsJsonAsync($"{_Host}{$"{configuration["Nidas:Endpoint:Verify"]!}"}", request);

                var result = await httpResponse.Content.ReadAsStringAsync();

            }
            else
            {

            }

            return authResponse!;



            //}
            //catch (TaskCanceledException ex)
            //{
            //    dtoResponse = new()
            //    {
            //        Status = "ERROR",
            //        Message = ex.Message
            //    };

            //    _logger.LogError(ex, "FundTransfer Socket Exception | uniqueIdentifier: {UniqueIdentifier} | Response: {@Request}", uId, dtoResponse);
            //}
            //catch (System.Net.Sockets.SocketException ex) when (ex.ErrorCode == 995)
            //{

            //    dtoResponse = new()
            //    {
            //        Status = "ERROR",
            //        Message = ex.Message
            //    };

            //    _logger.LogError(ex, "FundTransfer Socket Exception | uniqueIdentifier: {UniqueIdentifier} | Response: {@Request}", uId, dtoResponse);
            //}
            //catch (Exception ex)
            //{

            //    dtoResponse = new()
            //    {
            //        Status = "ERROR",
            //        Message = ex.Message
            //    };

            //    _logger.LogError(ex, "FundTransfer Unexpected Error | uniqueIdentifier: {UniqueIdentifier} | Response: {@Request}", uId, dtoResponse);

            //}

            //return dtoResponse;
        }



        private async Task<ResponseModel.Auth?> Authentication()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using HttpClient _client = new(handler) { Timeout = TimeSpan.FromSeconds(60)};

            var content = new RequestModel.Auth { ClientId = _ClientID, ClientSecret = _ClientSecret };

            var httpResponse = await _client.PostAsJsonAsync($"{_Host}{_Authentication}", content);

            var result = "";
            result = httpResponse.Content.ReadAsStringAsync().Result;
            
            if (httpResponse.IsSuccessStatusCode)
            {
                var response = JsonConvert.DeserializeObject<ResponseModel.Auth>(result);
                _logger.LogInformation("Authentication {Status} | Response: {response}", "SUCCESS", result);

                return response;
            }
            else if (httpResponse.StatusCode == HttpStatusCode.UnprocessableEntity) // 422 Validation Error
            {
                var validationErrors = JsonConvert.DeserializeObject<ResponseModel.AuthValidationErrorResponse>(result);
                _logger.LogWarning("Authentication {Status} | Validation Errors: {@Errors}", "FAILED_422", validationErrors?.Errors);

                return null;
            }
            else 
            {
                var authError = JsonConvert.DeserializeObject<ResponseModel.AuthErrorResponse>(result);
                _logger.LogError("Authentication {Status} | Error: {Error} - {Message}", "FAILED", authError?.Error, authError?.Message);

                return null; 
            }
        }


    }
}
