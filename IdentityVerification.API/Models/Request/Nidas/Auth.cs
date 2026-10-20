using System.Text.Json.Serialization;

namespace IdentityVerification.API.Models.Request.Nidas
{
    public class Auth
    {
        [JsonPropertyName("client_id")]
        public string ClientId { get; set; } = string.Empty;

        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; } = string.Empty;
    }
}
