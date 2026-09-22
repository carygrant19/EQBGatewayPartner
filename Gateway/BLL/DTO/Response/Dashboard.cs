namespace Gateway.BLL.DTO.Response
{
    public class DashboardSummary
    {
        // KPI Metrics Cards
        public long TotalRequestsToday { get; set; }
        public int ActiveRoutesCount { get; set; }
        public int ActiveClientsCount { get; set; }
        public double SuccessRatePercentage { get; set; }
        public double AverageLatencyMs { get; set; }
        public int UnhealthyHostsCount { get; set; }

        // Response Code Breakdown
        public int Status2xxCount { get; set; }
        public int Status4xxCount { get; set; }
        public int Status5xxCount { get; set; }

        // Charts & Analytics Data
        public List<HourlyTrafficDto> HourlyTraffic { get; set; } = [];
        public List<TopRouteMetricDto> TopRoutes { get; set; } = [];
        public List<TopClientMetricDto> TopClients { get; set; } = [];
        public List<RecentAuditLogDto> RecentLogs { get; set; } = [];
    }

    public class HourlyTrafficDto
    {
        public string Hour { get; set; } = string.Empty; // e.g., "08:00"
        public int Requests { get; set; }
        public int Errors { get; set; }
    }

    public class TopRouteMetricDto
    {
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public int RequestCount { get; set; }
        public double AvgLatencyMs { get; set; }
    }

    public class TopClientMetricDto
    {
        public string ClientCode { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public int RequestCount { get; set; }
    }

    public class RecentAuditLogDto
    {
        public string TraceId { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;
        public string Uri { get; set; } = string.Empty;
        public string ResponseCode { get; set; } = string.Empty;
        public double LatencyMs { get; set; }
        public DateTime RequestDate { get; set; }
    }
}