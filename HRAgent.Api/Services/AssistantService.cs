using HRAgent.Api.Models.Gemini;
using HRAgent.Api.Services.IService;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;
using DTO = HRAgent.Api.DTOs.Assistant;
using Model = HRAgent.Api.Models;

namespace HRAgent.Api.Services
{
    public class AssistantService(IConfiguration configuration, IWebHostEnvironment env, IMemoryCache cache, ILogger<AssistantService> logger) : IAssistantService
    {
        private readonly ILogger<AssistantService> _logger = logger;
        private readonly string _apiKey = configuration["Gemini:ApiKey"]!;
        private readonly string _endpoint = configuration["Gemini:Endpoint"]!;
        private readonly IWebHostEnvironment _env = env;
        private readonly IMemoryCache _cache = cache;

        private readonly Dictionary<string, string> _policyDocuments = new()
        {
            ["Officer"] = ExtractTextFromPdf(Path.Combine(env.ContentRootPath, "files", "policy-officer.pdf")),
            ["RankAndFile"] = ExtractTextFromPdf(Path.Combine(env.ContentRootPath, "files", "policy-rankfile.pdf"))
        };

        public async Task<string> Chat(DTO.Request.Chat dtoRequest)
        {

            var endpoint = $"{_endpoint}{_apiKey}";
            // 1. Determine which policy to use based on the user's rank
            if (!_policyDocuments.TryGetValue(dtoRequest.EmployeeRank, out string? relevantPolicyText))
            {
                return "Error: Invalid employee rank provided. Cannot locate the correct HR policy.";
            }

            // 2. Get the existing chat history from the cache, or start a new list
            if (!_cache.TryGetValue(dtoRequest.SessionId, out List<Model.Gemini.Content>? chatHistory))
            {
                chatHistory = [];
            }

            // 3. Append the user's NEW message to the history
            chatHistory!.Add(new Model.Gemini.Content
            {
                Role = "user",
                Parts = [new() { Text = dtoRequest.Message }]
            });


            string promptFilePath = Path.Combine(_env.ContentRootPath, "prompts", "assistant", "policy.txt");

            string promptTemplate = await System.IO.File.ReadAllTextAsync(promptFilePath);

            var promptText = string.Format(
               promptTemplate,
               dtoRequest.EmployeeRank,
               relevantPolicyText
           );

            var request = new Model.Gemini.Request
            {
                SystemInstruction = new Model.Gemini.Content
                {
                    Parts = [new() { Text = promptText }]
                },
                Contents = chatHistory 
            };

            // 5. Call the Gemini API

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            StringContent? content = new(System.Text.Json.JsonSerializer.Serialize(request, options), Encoding.UTF8, "application/json");

            using HttpClient client = new();
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.BaseAddress = new Uri(_endpoint);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");

            HttpResponseMessage httpResponse = await client.PostAsync(endpoint, content);
            httpResponse.EnsureSuccessStatusCode();

            var rawResponseString = await httpResponse.Content.ReadAsStringAsync();

            var jsonDoc = JObject.Parse(rawResponseString);

            var aiText = jsonDoc["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "";

            chatHistory.Add(new Model.Gemini.Content
            {
                Role = "model",
                Parts = [new() { Text = aiText }]
            });

            _cache.Set(dtoRequest.SessionId, chatHistory, TimeSpan.FromMinutes(30));

            return aiText;
        }

        private static string ExtractTextFromPdf(string filePath)
        {
            if (!File.Exists(filePath)) return "Policy document not found.";

            var sb = new StringBuilder();
            using (var pdf = PdfDocument.Open(filePath))
            {
                foreach (var page in pdf.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
            }
            return sb.ToString();
        }

    }
}
