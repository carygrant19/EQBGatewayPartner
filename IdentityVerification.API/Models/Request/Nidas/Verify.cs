using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IdentityVerification.API.Models.Request.Nidas
{
    public class Verify
    {
        [MaxLength(100)]
        [JsonPropertyName("first_name")]
        [JsonProperty("first_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        [JsonPropertyName("middle_name")]
        [JsonProperty("middle_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? MiddleName { get; set; }

        [MaxLength(100)]
        [JsonPropertyName("last_name")]
        [JsonProperty("last_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? LastName { get; set; }

        [MaxLength(20)]
        [JsonPropertyName("suffix")]
        [JsonProperty("suffix", NullValueHandling = NullValueHandling.Ignore)]
        public string? Suffix { get; set; }

        [JsonPropertyName("birth_date")]
        [JsonProperty("birth_date", NullValueHandling = NullValueHandling.Ignore)]
        public DateOnly? BirthDate { get; set; }

        [MaxLength(100)]
        [JsonPropertyName("face_liveness_session_id")]
        [JsonProperty("face_liveness_session_id", NullValueHandling = NullValueHandling.Ignore)]
        public string? FaceLivenessSessionId { get; set; }

        [JsonPropertyName("fingerprint")]
        [JsonProperty("fingerprint", NullValueHandling = NullValueHandling.Ignore)]
        public Fingerprint? Fingerprint { get; set; }
    }

    public sealed class Fingerprint
    {
        [Required]
        [JsonProperty("biometrics")]
        public List<Biometric> Biometrics { get; set; } = [];
    }

    public sealed class Biometric
    {
        [Required]
        [MaxLength(10)]
        [JsonProperty("specVersion")]
        public string SpecVersion { get; set; } = string.Empty;

        [Required]
        [JsonProperty("data")]
        public string Data { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        [JsonProperty("hash")]
        public string Hash { get; set; } = string.Empty;

        [Required]
        [JsonProperty("sessionKey")]
        public string SessionKey { get; set; } = string.Empty;

        [Required]
        [JsonProperty("error")]
        public BiometricError Error { get; set; } = new();

        [Required]
        [MaxLength(128)]
        [JsonProperty("thumbprint")]
        public string Thumbprint { get; set; } = string.Empty;
    }

    public sealed class BiometricError
    {
        [Required]
        [MaxLength(20)]
        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; } = string.Empty;

        [MaxLength(500)]
        [JsonProperty("errorInfo")]
        public string ErrorInfo { get; set; } = string.Empty;
    }
} 