namespace Gateway.BLL.DTO.Response
{
    public class VTransactionReport
    {
        public int CurrentPage { get; set; }
        public int TotalRecord { get; set; }
        public int TotalPage { get; set; }
        public int TotalSuccess2xx { get; set; }
        public int TotalError4xx5xx { get; set; }
        public double AverageLatencyMs { get; set; }
        public List<FTransactionReport> Data { get; set; } = [];
    }

    public class FTransactionReport
    {
        public long Id { get; set; }
        public DateTime RequestDate { get; set; }
        public string? ClientId { get; set; }
        public string HttpMethod { get; set; } = "GET";
        public string? TransactionId { get; set; }
        public string TraceId { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string Uri { get; set; } = string.Empty;
        public double LatencyMs { get; set; }
        public string ResponseCode { get; set; } = "200";
    }

    public class ReportRouteOption
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}