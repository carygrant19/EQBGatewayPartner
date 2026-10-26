using Common.Services.IService;
using IdentityVerification.API.Services.IService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Request = IdentityVerification.API.DTO.Request;
using RequestModel = IdentityVerification.API.Models.Request.Nidas;
using ResponseModel = IdentityVerification.API.Models.Response.Nidas;

namespace IdentityVerification.API.Services
{
    public class NidasService(
        IConfiguration configuration,
        ILogger<NidasService> logger,
        IHttpClientFactory httpClientFactory,
        ITransactionLogService transactionLogService) : INidasService
    {
        private readonly ILogger<NidasService> _logger = logger;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ITransactionLogService _transactionLogService = transactionLogService;

        private readonly string _Host = $"{configuration["Nidas:Host"]!}";
        private readonly string _ClientID = $"{configuration["Nidas:ClientID"]!}";
        private readonly string _ClientSecret = $"{configuration["Nidas:ClientSecret"]!}";
        private readonly string _Authentication = $"{configuration["Nidas:Endpoint:Authentication"]!}";
        private readonly string _Verify = $"{configuration["Nidas:Endpoint:Verify"]!}";
        private readonly string _QRQueryCheck = $"{configuration["Nidas:Endpoint:QRQueryCheck"]!}";
        private readonly string _QRQueryVerify = $"{configuration["Nidas:Endpoint:QRQueryVerify"]!}";

        public async Task<(int StatusCode, string ResponseBody, Dictionary<string, string[]> Headers)> Verify(Request.VerifyRequest dtoRequest)
        {
            RequestModel.Verify modelRequest = new()
            {
                FirstName = dtoRequest.FirstName,
                LastName = dtoRequest.LastName,
                MiddleName = dtoRequest.MiddleName,
                Suffix = dtoRequest.Suffix,
                BirthDate = dtoRequest.BirthDate,
                FaceLivenessSessionId = dtoRequest.FaceLivenessSessionId,
                Fingerprint = dtoRequest.Fingerprint is not null ? new RequestModel.Fingerprint
                {
                    Biometrics = dtoRequest.Fingerprint.Biometrics.Select(b => new RequestModel.Biometric
                    {
                        SpecVersion = b.SpecVersion,
                        Data = b.Data,
                        Hash = b.Hash,
                        SessionKey = b.SessionKey,
                        Error = b.Error,
                        Thumbprint = b.Thumbprint
                    }).ToList()
                } : null
            };

            string transactionId = dtoRequest.FaceLivenessSessionId ?? Guid.NewGuid().ToString("N");
            return await SendNidasRequest(_Verify, modelRequest, transactionId);
        }

        public async Task<(int StatusCode, string ResponseBody, Dictionary<string, string[]> Headers)> QRQueryCheck(Request.QrCheckRequest dtoRequest)
        {
            RequestModel.QrCheck modelRequest = new()
            {
                Value = dtoRequest.Value
            };

            return await SendNidasRequest(_QRQueryCheck, modelRequest, dtoRequest.Value);
        }

        public async Task<(int StatusCode, string ResponseBody, Dictionary<string, string[]> Headers)> QRQueryVerify(Request.QrVerifyRequest dtoRequest)
        {
            RequestModel.QrVerify modelRequest = new()
            {
                Value = dtoRequest.Value,
                FaceLivenessSessionId = dtoRequest.FaceLivenessSessionId,
                Fingerprint = dtoRequest.Fingerprint is not null ? new RequestModel.Fingerprint
                {
                    Biometrics = dtoRequest.Fingerprint.Biometrics.Select(b => new RequestModel.Biometric
                    {
                        SpecVersion = b.SpecVersion,
                        Data = b.Data,
                        Hash = b.Hash,
                        SessionKey = b.SessionKey,
                        Error = b.Error,
                        Thumbprint = b.Thumbprint
                    }).ToList()
                } : null
            };

            string transactionId = dtoRequest.FaceLivenessSessionId ?? dtoRequest.Value;
            return await SendNidasRequest(_QRQueryVerify, modelRequest, transactionId);
        }

        private async Task<(int StatusCode, string ResponseBody, Dictionary<string, string[]> Headers)> SendNidasRequest<TModelRequest>(
            string endpoint,
            TModelRequest modelRequest,
            string? transactionId = null)
        {
            try
            {
                var authResponse = await Authentication();

                if (authResponse?.Data?.AccessToken is null)
                {
                    _logger.LogError("NIDAS Request Aborted: Walang valid bearer token.");
                    return (
                        (int)HttpStatusCode.Unauthorized,
                        "{\"error\":\"unauthorized\",\"message\":\"Failed to acquire internal bearer token from NIDAS.\"}",
                        new Dictionary<string, string[]>()
                    );
                }

                var client = _httpClientFactory.CreateClient("ExternalClient");
                client.Timeout = TimeSpan.FromSeconds(60);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.Data.AccessToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (!string.IsNullOrEmpty(transactionId))
                {
                    client.DefaultRequestHeaders.Add("uniqueIdentifier", transactionId);
                }

                var requestUrl = $"{_Host.TrimEnd('/')}/{endpoint.TrimStart('/')}";
                _logger.LogInformation("Calling NIDAS Endpoint: {Url} | Payload: {@Payload}", requestUrl, modelRequest);

                var httpResponse = await client.PostAsJsonAsync(requestUrl, modelRequest);
                var rawJson = await httpResponse.Content.ReadAsStringAsync();
                var statusCode = (int)httpResponse.StatusCode;

                var headers = httpResponse.Headers
                    .Concat(httpResponse.Content.Headers)
                    .ToDictionary(h => h.Key, h => h.Value.ToArray());

                _logger.LogInformation("NIDAS Response ({StatusCode}) | Body: {Result}", statusCode, rawJson);

                return (statusCode, rawJson, headers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling NIDAS endpoint {Endpoint}", endpoint);
                await _transactionLogService.LogExceptionAsync($"NidasService.{endpoint}", ex);
                throw;
            }
        }

        private async Task<ResponseModel.Auth?> Authentication()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ExternalClient");
                client.Timeout = TimeSpan.FromSeconds(60);
                client.DefaultRequestHeaders.Add("uniqueIdentifier", _ClientID);

                var content = new RequestModel.Auth
                {
                    ClientId = _ClientID,
                    ClientSecret = _ClientSecret
                };

                var authUrl = $"{_Host.TrimEnd('/')}/{_Authentication.TrimStart('/')}";
                var httpResponse = await client.PostAsJsonAsync(authUrl, content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var response = await httpResponse.Content.ReadFromJsonAsync<ResponseModel.Auth>();
                    _logger.LogInformation("NIDAS Authentication SUCCESS.");
                    return response;
                }

                var err = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogError("NIDAS Auth Failed ({StatusCode}) | Response: {Response}", httpResponse.StatusCode, err);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during NIDAS Authentication.");
                await _transactionLogService.LogExceptionAsync("NidasService.Authentication", ex);
                return null;
            }
        }
    }
}