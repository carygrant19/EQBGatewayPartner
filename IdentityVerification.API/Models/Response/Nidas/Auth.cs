using Newtonsoft.Json;

namespace IdentityVerification.API.Models.Response.Nidas
{
    public class Auth
    {
        [JsonProperty("data")]
        public AuthData Data { get; set; } 
    }

    public class AuthData
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonProperty("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonProperty("expires_at")]
        public string ExpiresAt { get; set; } = string.Empty;
    }

    //422
    public class AuthValidationErrorResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
        
        [JsonProperty("errors")]
        public Dictionary<string, List<string>> Errors { get; set; }
    }

    //403
    public class AuthErrorResponse
    {
        [JsonProperty("error")]
        public string Error { get; set; } = string.Empty;

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("error_description")]
        public string ErrorDescription { get; set; } = string.Empty;
    }

}
