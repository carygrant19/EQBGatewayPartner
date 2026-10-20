using System.Text.Json.Serialization;

namespace IdentityVerification.API.Models.Response.Nidas
{
    //422
    internal class ValidationErrorResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("errors")]
        public Dictionary<string, List<string>> Errors { get; set; }
    }

    //403
    internal class ErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("error_description")]
        public string ErrorDescription { get; set; } = string.Empty;
    }
}
