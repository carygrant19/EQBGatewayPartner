using Newtonsoft.Json;

namespace Temenos.API.Models.v1.Response
{
    internal class balanceInquiry
    {
        [JsonProperty("header")]
        public balanceInquiryHeader Header { get; set; } = new();
        [JsonProperty("body")]
        public List<balanceInquiryBody> Body { get; set; } = new();

        [JsonProperty("error")]
        public BalanceInquiryError Error { get; set; } = new();
    }
    internal class balanceInquiryHeader
    {
        [JsonProperty("audit")]
        public balanceInquiryAudit Audit { get; set; } = new();

        [JsonProperty("page_start")]
        public int PageStart { get; set; } = 0;

        [JsonProperty("page_token")]
        public string PageToken { get; set; } = string.Empty;

        [JsonProperty("total_size")]
        public int TotalSize { get; set; } = 0;

        [JsonProperty("page_size")]
        public int PageSize { get; set; } = 0;
    }
    internal class balanceInquiryAudit
    {
        [JsonProperty("T24_time")]
        public int T24Time { get; set; } = 0;

        [JsonProperty("parse_time")]
        public int ParseTime { get; set; } = 0;
    }
    internal class balanceInquiryBody
    {
        [JsonProperty("product")]
        public string Product { get; set; } = string.Empty;
        [JsonProperty("balance")]
        public decimal Balance { get; set; } = 0;
        [JsonProperty("accountName")]
        public string AccountName { get; set; } = string.Empty;
        [JsonProperty("accountType")]
        public string AccountType { get; set; } = string.Empty;
        [JsonProperty("currency")]
        public string Currency { get; set; } = string.Empty;
        [JsonProperty("classification")]
        public string Classification { get; set; } = string.Empty;
    }

    internal class BalanceInquiryError
    {
        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty;
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;
    }
}
