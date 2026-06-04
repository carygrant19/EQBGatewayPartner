using System.Text.Json.Serialization;

namespace HRAgent.Api.Models.Gemini
{
    internal class Request
    {
        [JsonPropertyName("systemInstruction")]
        public Content? SystemInstruction { get; set; }

        [JsonPropertyName("contents")]
        public List<Content> Contents { get; set; } = [];
    }

    internal class Content
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; } = [];
    }

    internal class Part
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

}
