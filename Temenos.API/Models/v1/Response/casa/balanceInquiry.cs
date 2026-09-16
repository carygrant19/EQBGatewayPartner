using System.Text.Json.Serialization;

namespace Temenos.API.Models.v1.Response.casa
{
    internal class balanceInquiry
    {
        [JsonPropertyName("header")]
        public balanceInquiryHeader Header { get; set; } = new();
        [JsonPropertyName("body")]
        public List<balanceInquiryBody> Body { get; set; } = new();

        [JsonPropertyName("error")]
        public BalanceInquiryError Error { get; set; } = new();
    }
    internal class balanceInquiryHeader
    {
        [JsonPropertyName("audit")]
        public balanceInquiryAudit Audit { get; set; } = new();

        [JsonPropertyName("page_start")]
        public int PageStart { get; set; } = 0;

        [JsonPropertyName("page_token")]
        public string PageToken { get; set; } = string.Empty;

        [JsonPropertyName("total_size")]
        public int TotalSize { get; set; } = 0;

        [JsonPropertyName("page_size")]
        public int PageSize { get; set; } = 0;
    }
    internal class balanceInquiryAudit
    {
        [JsonPropertyName("T24_time")]
        public int T24Time { get; set; } = 0;

        [JsonPropertyName("parse_time")]
        public int ParseTime { get; set; } = 0;
    }
    internal class balanceInquiryBody
    {
        [JsonPropertyName("product")]
        public string Product { get; set; } = string.Empty;
        [JsonPropertyName("balance")]
        public decimal Balance { get; set; } = 0;
        [JsonPropertyName("accountName")]
        public string AccountName { get; set; } = string.Empty;
        [JsonPropertyName("accountType")]
        public string AccountType { get; set; } = string.Empty;
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;
        [JsonPropertyName("classification")]
        public string Classification { get; set; } = string.Empty;
    }

    internal class BalanceInquiryError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }
}
