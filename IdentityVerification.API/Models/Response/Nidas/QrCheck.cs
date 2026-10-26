using System.Text.Json.Serialization;

namespace IdentityVerification.API.Models.Response.Nidas
{
    public class QrCheck
    {
        [JsonPropertyName("data")]
        public QrCheckData? Data { get; set; }

        [JsonPropertyName("meta")]
        public QrCheckMeta? Meta { get; set; }
    }

    public class QrCheckData
    {
        [JsonPropertyName("digital_id")]
        public string? DigitalId { get; set; }
    }

    public class QrCheckMeta
    {
        [JsonPropertyName("qr_type")]
        public string? QrType { get; set; }
    }
}