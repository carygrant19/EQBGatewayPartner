
using System.Text.Json.Serialization;

namespace IdentityVerification.API.Models.Response.Nidas
{
    public class Auth
    {
        [JsonPropertyName("data")]
        public AuthData Data { get; set; } = new();
    }

    public class AuthData
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("expires_at")]
        public string ExpiresAt { get; set; } = string.Empty;
    }

    // 422 Unprocessable Content
    public class AuthValidationErrorResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("errors")]
        public Dictionary<string, List<string>> Errors { get; set; } = [];
    }

    // 403 Forbidden
    public class AuthErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }
    }
}