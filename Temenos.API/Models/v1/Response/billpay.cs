using System.Text.Json.Serialization;

namespace Temenos.API.Models.v1.Response
{
    internal class billpay
    {
        [JsonPropertyName("header")]
        public billpayResponseHead Header { get; set; } = new();
        [JsonPropertyName("error")]
        public billpayResponseError Error { get; set; } = new();
    }

    internal class billpayResponseHead
    {
        [JsonPropertyName("transactionStatus")]
        public string TransactionStatus { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    internal class billpayResponseError
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        [JsonPropertyName("errorDetails")]
        public List<billpayResponseErrorDetails> ErrorDetails { get; set; } = default!;
    }

    internal class billpayResponseErrorDetails
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

}
