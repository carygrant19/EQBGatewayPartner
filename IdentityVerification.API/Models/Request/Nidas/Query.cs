using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IdentityVerification.API.Models.Request.Nidas
{
    public class QrCheck
    {
        [Required]
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }

    public class QrVerify
    {
        [Required]
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("face_liveness_session_id")]
        public string? FaceLivenessSessionId { get; set; }

        [JsonPropertyName("fingerprint")]
        public Fingerprint? Fingerprint { get; set; }
    }
}