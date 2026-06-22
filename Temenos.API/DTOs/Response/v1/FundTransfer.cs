namespace Temenos.API.DTOs.Response.v1
{
    public class FundTransfer
    {
        public string TransactionId { get; set; } = string.Empty;
        public string ReferenceNo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; } = 0;
        public string DebitCurrency { get; set; } = "PHP";
        public string Creditcurrency { get; set; } = "PHP";
        public DateTime ProcessedAt { get; set; } = default;
    }
}
