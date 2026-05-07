using System.Text.Json.Serialization;

namespace Temenos.API.Models
{
    internal class TransactionRequest
    {
        [JsonPropertyName("body")]
        public TransactionRequestBody Body { get; set; } = new TransactionRequestBody();
    }

    internal class TransactionRequestBody
    {
        [JsonPropertyName("transactionType")]
        public string TransactionType { get; set; } = string.Empty!;

        [JsonPropertyName("debValDate")]
        public string DebValDate { get; set; } = string.Empty!;

        [JsonPropertyName("debitAmount")]
        public string DebitAmount { get; set; } = string.Empty!;
        [JsonPropertyName("debitAccountNumber")]

        public string DebitAccountNumber { get; set; } = string.Empty!;

        [JsonPropertyName("debitCurrency")]
        public string DebitCurrency { get; set; } = "PHP";

        [JsonPropertyName("creValDate")]
        public string CreValDate { get; set; } = string.Empty!;

        [JsonPropertyName("creditAccountNumber")]
        public string CreditAccountNumber { get; set; } = string.Empty!;

        [JsonPropertyName("creditCurrency")]
        public string CreditCurrency { get; set; } = "PHP";

        [JsonPropertyName("orderingCust")]
        public string OrderingCust { get; set; } = "PAYMENT";

        [JsonPropertyName("orderingBank")]
        public string OrderingBank { get; set; } = "EQB";

        [JsonPropertyName("debitTheirRef")]
        public string DebitTheirRef { get; set; } = string.Empty!;

        [JsonPropertyName("creditTheirRef")]
        public string CreditTheirRef { get; set; } = string.Empty!;
    }

}
