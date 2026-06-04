using Newtonsoft.Json;
using Notification.API.Services.IService;
using System.Text;
using DTO = Notification.API.DTOs;
using Model = Notification.API.Models;

namespace Notification.API.Services
{
    public class SMSService(IConfiguration configuration, ILogger<SMSService> logger) : ISMSService
    {
        private readonly ILogger<SMSService> _logger = logger;

        public async Task<string> Send(
             Model.SMSConfig config,
             DTO.Request.Sms request
        )
        {
            string result = "";

            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                using HttpClient client = new(handler)
                {
                    Timeout = Timeout.InfiniteTimeSpan
                };

                _logger.LogInformation("Sending email | Payload: {@request}", System.Text.Json.JsonSerializer.Serialize(request));

                client.DefaultRequestHeaders.Add("eos-api-key", configuration["SMSConfig:ApiKey"]!);

                StringContent content = new(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                HttpResponseMessage responseMessage;

                responseMessage = await client.PostAsync(configuration["Endpoints:Globe360"]!, content);

                if (responseMessage.ReasonPhrase == "OK")
                {
                    _logger.LogInformation("Mail sent");
                    result = "success";
                }
                else
                {
                    result = responseMessage.ReasonPhrase!;
                    _logger.LogError(responseMessage.ReasonPhrase, "Sending Failed");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sending Failed");
                throw new ApplicationException("Exception has occurred: " + ex.Message);
            }

            return result;
        }

    }
}
