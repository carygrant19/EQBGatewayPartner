using System.Text.Json.Serialization;

namespace Temenos.API.Models
{
    internal class TransactionResponse
    {
        [JsonPropertyName("header")]
        public TransactionResponseHead Header { get; set; } = new();
        [JsonPropertyName("error")]
        public TransactionResponseError Error { get; set; } = new();
    }

    internal class TransactionResponseHead
    {
        [JsonPropertyName("transactionStatus")]
        public string TransactionStatus { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class TransactionResponseError
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        [JsonPropertyName("errorDetails")]
        public List<TransactionResponseErrorDetails> ErrorDetails { get; set; } = default!;
    }

    public class TransactionResponseErrorDetails
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

}
