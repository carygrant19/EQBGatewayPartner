using System;
using System.Collections.Generic;

namespace Gateway.BLL.DTO.Response
{
    public class DashboardSummaryDto
    {
        // 1. Gateway Infrastructure State
        public int ActiveRoutesCount { get; set; }
        public int ActiveClientsCount { get; set; }
        public int UnhealthyHostsCount { get; set; }

        // 2. Traffic Volume & Performance (mula sa Log_Http)
        public long TotalRequestsToday { get; set; }
        public int Status2xxCount { get; set; }
        public int Status4xxCount { get; set; }
        public int Status5xxCount { get; set; }
        public double SuccessRatePercentage { get; set; }
        public double AverageLatencyMs { get; set; }

        // 3. Analytics & Rankings
        public List<HourlyTrafficDto> HourlyTraffic { get; set; } = [];
        public List<TopRouteMetricDto> TopRoutes { get; set; } = [];

        // 4. Correlated Telemetry Feed (Pinagsamang Log_Http at Log_Transaction)
        public List<RecentAuditLogDto> RecentLogs { get; set; } = [];
        public List<RecentExceptionDto> RecentExceptions { get; set; } = [];
    }

    public class HourlyTrafficDto
    {
        public string Hour { get; set; } = string.Empty; // Hal. "08:00"
        public int Requests { get; set; }
        public int Errors { get; set; }
    }

    public class TopRouteMetricDto
    {
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public int RequestCount { get; set; }
    }

    public class RecentAuditLogDto
    {
        public long Id { get; set; }
        public string? TransactionId { get; set; }
        public string TraceId { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = "GET";
        public string Uri { get; set; } = string.Empty;
        public string ResponseCode { get; set; } = "200";
        public double LatencyMs { get; set; }
        public string RequestDate { get; set; } = string.Empty;
    }

    public class RecentExceptionDto
    {
        public long Id { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
    }
}