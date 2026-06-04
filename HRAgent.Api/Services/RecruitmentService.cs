using HRAgent.Api.Services.IService;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json;
using DTO = HRAgent.Api.DTOs.Recruitment;
using Model = HRAgent.Api.Models;

namespace HRAgent.Api.Services
{
    public class RecruitmentService(IConfiguration configuration, IWebHostEnvironment env, ILogger<RecruitmentService> logger) : IRecruitmentService
    {
        private readonly ILogger<RecruitmentService> _logger = logger;
        private readonly string _apiKey = configuration["Gemini:ApiKey"]!;
        private readonly string _endpoint = configuration["Gemini:Endpoint"]!;
        private readonly IWebHostEnvironment _env = env;
        public async Task<DTO.Response.Evaluate?> EvaluateCandidate(DTO.Request.Evaluate dtoRequest)
        {

            string promptFilePath = Path.Combine(_env.ContentRootPath, "prompts", "recruitment", $"{dtoRequest.JobType.ToLower()}.txt" );

            string promptTemplate = await System.IO.File.ReadAllTextAsync(promptFilePath);

            var promptText = string.Format(
               promptTemplate,
               dtoRequest.JobDescription,
               dtoRequest.Profile
           );

            var endpoint = $"{_endpoint}{_apiKey}";

            Model.Gemini.Request request = new()
            {
                Contents =
                [
                    new() {
                        Parts =
                        [
                            new Model.Gemini.Part { Text = promptText }
                        ]
                    }
                ]
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            StringContent? content = new(System.Text.Json.JsonSerializer.Serialize(request, options), Encoding.UTF8, "application/json");

            using HttpClient client = new();
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.BaseAddress = new Uri(endpoint);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");

            HttpResponseMessage httpResponse = await client.PostAsync(endpoint, content);
            httpResponse.EnsureSuccessStatusCode();

            var rawResponseString = await httpResponse.Content.ReadAsStringAsync();

            var jsonDoc = JObject.Parse(rawResponseString);

            var aiText = jsonDoc["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "";

            var cleanJson = aiText.Replace("```json", "").Replace("```", "").Trim();

            var dtoResponse = JsonConvert.DeserializeObject<DTO.Response.Evaluate>(cleanJson);

            return dtoResponse;
        }





    }
}
