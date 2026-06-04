using Microsoft.AspNetCore.SignalR;
using System.Text;
using System.Text.Json;

namespace Chatbot.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ChatHub(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task SendMessage(string message)
        {
            var client = _httpClientFactory.CreateClient();

            var requestBody = new
            {
                sessionId = "11111",
                message = message,
                employeeRank = "RankAndFile"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync("https://localhost:7229/api/assistant/policy", content);

            if (!response.IsSuccessStatusCode)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "Error calling API");
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            //var result = JsonSerializer.Deserialize<ChatResponse>(json);

            await Clients.Caller.SendAsync("ReceiveMessage", json ?? "No response");
        }
    }

    public class ChatResponse
    {
        public string reply { get; set; }
    }
}