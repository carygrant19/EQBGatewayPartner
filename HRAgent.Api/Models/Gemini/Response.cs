using System.Text.Json.Serialization;

namespace HRAgent.Api.Models.Gemini
{
    internal class Response
    {
        [JsonPropertyName("candidates")]
        public List<Response>? Candidates { get; set; }
    }
    internal class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }
}
