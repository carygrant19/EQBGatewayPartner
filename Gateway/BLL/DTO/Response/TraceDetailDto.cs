using System.Collections.Generic;

namespace Gateway.BLL.DTO.Response
{
    public class TraceDetailDto
    {
        public string TraceId { get; set; } = string.Empty;
        public string OverallStatus { get; set; } = "SUCCESS";
        public double TotalDurationMs { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public List<TraceTimelineHopDto> Hops { get; set; } = [];
    }

    public class TraceTimelineHopDto
    {
        public int Step { get; set; }
        public string HopType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? TransactionId { get; set; } // Dito na nakatira ang TransactionId per hop
        public string Endpoint { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = "POST";
        public string Status { get; set; } = "200";
        public double DurationMs { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public string? RequestPayload { get; set; }
        public string? ResponsePayload { get; set; }
        public bool IsError { get; set; }
    }
}