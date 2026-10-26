
using System.Text.Json.Serialization;

namespace IdentityVerification.API.Models.Response.Nidas
{
    internal class ValidationErrorResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("errors")]
        public Dictionary<string, List<string>> Errors { get; set; } = [];
    }

    internal class ErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }
    }
}