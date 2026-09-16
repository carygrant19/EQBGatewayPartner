using System.Text.Json.Serialization;

namespace Temenos.API.Models.v1.Response.loans
{
    internal class amortizationSchedule
    {
        [JsonPropertyName("header")]
        public amortizationScheduleHeader Header { get; set; } = new();
        [JsonPropertyName("body")]
        public List<amortizationScheduleBody> Body { get; set; } = new();

        [JsonPropertyName("error")]
        public amortizationScheduleError Error { get; set; } = new();
    }

    internal class amortizationScheduleHeader
    {
        [JsonPropertyName("audit")]
        public amortizationScheduleAudit Audit { get; set; } = new();

        [JsonPropertyName("page_start")]
        public int PageStart { get; set; } = 0;

        [JsonPropertyName("page_token")]
        public string PageToken { get; set; } = string.Empty;

        [JsonPropertyName("total_size")]
        public int TotalSize { get; set; } = 0;

        [JsonPropertyName("page_size")]
        public int PageSize { get; set; } = 0;
    }
    internal class amortizationScheduleAudit
    {
        [JsonPropertyName("T24_time")]
        public int T24Time { get; set; } = 0;

        [JsonPropertyName("parse_time")]
        public int ParseTime { get; set; } = 0;
    }
    internal class amortizationScheduleBody
    {
        [JsonPropertyName("principal")]
        public string? principal { get; set; }
        [JsonPropertyName("outstanding")]
        public string? outstanding { get; set; }
        [JsonPropertyName("interest")]
        public string? interest { get; set; }
        [JsonPropertyName("dueDate")]
        public string? dueDate { get; set; }
        [JsonPropertyName("totalDue")]
        public string? totalDue { get; set; }
    }

    internal class amortizationScheduleError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }
}
