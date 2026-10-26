using Gateway.BLL.DTO.Response;
using Gateway.BLL.Helper;
using Gateway.BLL.Services.IServices;
using Gateway.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Model = Gateway.Data.Models;

namespace Gateway.BLL.Services
{
    public class DashboardService(EFDbContext dbContext) : IDashboardService
    {
        private readonly EFDbContext _dbContext = dbContext;

        public async Task<DashboardSummaryDto> GetDashboardMetricsAsync(CancellationToken cancellationToken = default)
        {
            var summary = new DashboardSummaryDto();
            var today = DateTime.Today;

            try
            {
                // 1. Gateway Infrastructure State
                summary.ActiveRoutesCount = await _dbContext.Set<Model.Route>()
                    .CountAsync(r => r.IsActive, cancellationToken);

                summary.ActiveClientsCount = await _dbContext.Set<Model.Client>()
                    .CountAsync(c => !c.Deleted && c.Status == "Active", cancellationToken);

                summary.UnhealthyHostsCount = await _dbContext.Set<Model.TargetHost>()
                    .CountAsync(h => !h.IsHealthy, cancellationToken);

                // 2. HTTP Traffic Aggregations (Log_Http)
                var todayLogsQuery = _dbContext.Set<Model.HttpLog>()
                    .Where(l => l.RequestDate >= today)
                    .AsNoTracking();

                summary.TotalRequestsToday = await todayLogsQuery.CountAsync(cancellationToken);

                if (summary.TotalRequestsToday > 0)
                {
                    summary.Status2xxCount = await todayLogsQuery
                        .CountAsync(l => l.ResponseCode != null && l.ResponseCode.StartsWith("2"), cancellationToken);

                    summary.Status4xxCount = await todayLogsQuery
                        .CountAsync(l => l.ResponseCode != null && l.ResponseCode.StartsWith("4"), cancellationToken);

                    summary.Status5xxCount = await todayLogsQuery
                        .CountAsync(l => l.ResponseCode != null && l.ResponseCode.StartsWith("5"), cancellationToken);

                    int totalErrors = summary.Status4xxCount + summary.Status5xxCount;
                    summary.SuccessRatePercentage = Math.Round(((double)(summary.TotalRequestsToday - totalErrors) / summary.TotalRequestsToday) * 100, 2);

                    var avgDuration = await todayLogsQuery
                        .Where(l => l.ResponseDate.HasValue)
                        .Select(l => (double?)EF.Functions.DateDiffMillisecond(l.RequestDate, l.ResponseDate!.Value))
                        .AverageAsync(cancellationToken);

                    summary.AverageLatencyMs = Math.Round(avgDuration ?? 0, 2);

                    var rawHourly = await todayLogsQuery
                        .GroupBy(l => l.RequestDate.Hour)
                        .Select(g => new
                        {
                            Hour = g.Key,
                            Requests = g.Count(),
                            Errors = g.Count(l => l.ResponseCode != null && (l.ResponseCode.StartsWith("4") || l.ResponseCode.StartsWith("5")))
                        })
                        .OrderBy(g => g.Hour)
                        .ToListAsync(cancellationToken);

                    summary.HourlyTraffic = rawHourly.Select(g => new HourlyTrafficDto
                    {
                        Hour = $"{g.Hour:D2}:00",
                        Requests = g.Requests,
                        Errors = g.Errors
                    }).ToList();

                    var topRouteGroups = await todayLogsQuery
                        .Where(l => !string.IsNullOrEmpty(l.RouteId))
                        .GroupBy(l => l.RouteId)
                        .Select(g => new { RouteId = g.Key, Count = g.Count() })
                        .OrderByDescending(g => g.Count)
                        .Take(5)
                        .ToListAsync(cancellationToken);

                    var routeIds = topRouteGroups
                        .Where(r => r.RouteId != null && long.TryParse(r.RouteId, out _))
                        .Select(r => Convert.ToInt64(r.RouteId))
                        .ToList();

                    if (routeIds.Count > 0)
                    {
                        var routesMap = await _dbContext.Set<Model.Route>()
                            .Where(r => routeIds.Contains(r.Id))
                            .ToDictionaryAsync(r => r.Id.ToString(), r => new { r.Code, r.Name }, cancellationToken);

                        foreach (var item in topRouteGroups)
                        {
                            if (item.RouteId != null && routesMap.TryGetValue(item.RouteId, out var routeInfo))
                            {
                                summary.TopRoutes.Add(new TopRouteMetricDto
                                {
                                    RouteCode = routeInfo.Code,
                                    RouteName = routeInfo.Name,
                                    RequestCount = item.Count
                                });
                            }
                        }
                    }

                    var recentHttpLogs = await todayLogsQuery
                        .OrderByDescending(l => l.RequestDate)
                        .Take(10)
                        .Select(l => new
                        {
                            l.Id,
                            l.TraceId,
                            l.HttpMethod,
                            l.Uri,
                            l.ResponseCode,
                            Latency = l.ResponseDate.HasValue ? (double?)EF.Functions.DateDiffMillisecond(l.RequestDate, l.ResponseDate.Value) : 0,
                            l.RequestDate
                        })
                        .ToListAsync(cancellationToken);

                    var traceIds = recentHttpLogs
                        .Where(l => !string.IsNullOrEmpty(l.TraceId))
                        .Select(l => l.TraceId!)
                        .Distinct()
                        .ToList();

                    var tranMap = new Dictionary<string, string>();
                    if (traceIds.Count > 0)
                    {
                        tranMap = await _dbContext.Set<Model.TransactionLog>()
                            .Where(t => traceIds.Contains(t.TraceId!) && !string.IsNullOrEmpty(t.TransactionId))
                            .GroupBy(t => t.TraceId!)
                            .Select(g => new { TraceId = g.Key, TransactionId = g.Select(x => x.TransactionId).FirstOrDefault() })
                            .ToDictionaryAsync(x => x.TraceId, x => x.TransactionId ?? string.Empty, cancellationToken);
                    }

                    summary.RecentLogs = recentHttpLogs.Select(l => new RecentAuditLogDto
                    {
                        Id = l.Id,
                        TransactionId = (!string.IsNullOrEmpty(l.TraceId) && tranMap.TryGetValue(l.TraceId, out var tranId)) ? tranId : null,
                        TraceId = l.TraceId ?? string.Empty,
                        HttpMethod = l.HttpMethod ?? "GET",
                        Uri = l.Uri ?? string.Empty,
                        ResponseCode = l.ResponseCode ?? "200",
                        LatencyMs = l.Latency ?? 0,
                        RequestDate = l.RequestDate.ToString("HH:mm:ss")
                    }).ToList();
                }
                 
                summary.RecentExceptions = await _dbContext.Set<Model.ExceptionLog>()
                    .OrderByDescending(e => e.LogDate)
                    .Take(5)
                    .Select(e => new RecentExceptionDto
                    {
                        Id = e.Id,
                        ModuleName = e.ModuleName ?? "UNKNOWN",
                        Message = e.Message ?? string.Empty,
                        Source = e.Source,
                        StackTrace = e.StackTrace,
                        InnerException = e.InnerException,
                        Time = e.LogDate.ToString("HH:mm:ss"),
                        FullDate = e.LogDate.ToString("yyyy-MM-dd HH:mm:ss")
                    })
                    .ToListAsync(cancellationToken);
            }
            catch
            {
            }

            return summary;
        }

        public async Task<TraceDetailDto?> GetTraceTimelineAsync(string traceId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(traceId)) return null;

            var httpLogs = await _dbContext.Set<Model.HttpLog>()
                .Where(h => h.TraceId == traceId)
                .AsNoTracking()
                .OrderBy(h => h.RequestDate)
                .ToListAsync(cancellationToken);

            var tranLogs = await _dbContext.Set<Model.TransactionLog>()
                .Where(t => t.TraceId == traceId)
                .AsNoTracking()
                .OrderBy(t => t.RequestDate)
                .ToListAsync(cancellationToken);

            if (httpLogs.Count == 0 && tranLogs.Count == 0) return null;

            var firstHttp = httpLogs.FirstOrDefault();
            var firstTran = tranLogs.FirstOrDefault();

            var detail = new TraceDetailDto
            {
                TraceId = traceId,
                TransactionId = tranLogs.FirstOrDefault(t => !string.IsNullOrEmpty(t.TransactionId))?.TransactionId,
                StartTime = (firstHttp?.RequestDate ?? firstTran?.RequestDate ?? DateTime.Now).ToString("yyyy-MM-dd HH:mm:ss.fff")
            };

            int step = 1;

            // 1. Gateway Entry Hop
            if (firstHttp != null)
            {
                double ingressLatency = firstHttp.ResponseDate.HasValue
                    ? Math.Max((firstHttp.ResponseDate.Value - firstHttp.RequestDate).TotalMilliseconds, 0)
                    : 0;

                bool isErr = firstHttp.ResponseCode != null && (firstHttp.ResponseCode.StartsWith("4") || firstHttp.ResponseCode.StartsWith("5"));

                detail.Hops.Add(new TraceTimelineHopDto
                {
                    Step = step++,
                    HopType = "GATEWAY_INGRESS",
                    Title = "Gateway Proxy Ingress",
                    Endpoint = firstHttp.Uri ?? "/unknown",
                    HttpMethod = firstHttp.HttpMethod ?? "GET",
                    Status = firstHttp.ResponseCode ?? "200",
                    DurationMs = Math.Round(ingressLatency, 2),
                    Timestamp = firstHttp.RequestDate.ToString("HH:mm:ss.fff"),
                    RequestPayload = firstHttp.RequestData,
                    ResponsePayload = firstHttp.ResponseData,
                    IsError = isErr
                });

                detail.TotalDurationMs = Math.Round(ingressLatency, 2);
                if (isErr) detail.OverallStatus = "FAILED";
            }

            // 2. Microservice Hops (Temenos, InstaPay, PESONet, etc.)
            foreach (var tran in tranLogs)
            {
                double tranLatency = (tran.RequestDate.HasValue && tran.ResponseDate.HasValue)
                    ? Math.Max((tran.ResponseDate.Value - tran.RequestDate.Value).TotalMilliseconds, 0)
                    : 0;

                bool isTranErr = tran.Status != "SUCCESS";

                detail.Hops.Add(new TraceTimelineHopDto
                {
                    Step = step++,
                    HopType = "MICROSERVICE_CALL",
                    Title = ResolveServiceTitle(tran.UrlEndpoint),
                    Endpoint = tran.UrlEndpoint ?? "Downstream Service",
                    HttpMethod = "POST",
                    Status = tran.Status ?? "PENDING",
                    DurationMs = Math.Round(tranLatency, 2),
                    Timestamp = (tran.RequestDate ?? DateTime.Now).ToString("HH:mm:ss.fff"),
                    RequestPayload = tran.Request,
                    ResponsePayload = tran.Response,
                    IsError = isTranErr
                });

                if (isTranErr) detail.OverallStatus = "FAILED";
            }

            return detail;
        }

        private static string ResolveServiceTitle(string? endpoint)
        {
            if (string.IsNullOrEmpty(endpoint)) return "Downstream Microservice";
            if (endpoint.Contains("/casa/") || endpoint.Contains("/loans/") || endpoint.StartsWith("SQL:")) return "Temenos Core Banking Service";
            if (endpoint.Contains("/nidas/") || endpoint.Contains("/everify")) return "Nidas Identity Verification";
            if (endpoint.Contains("/instapay")) return "InstaPay Microservice";
            if (endpoint.Contains("/pesonet")) return "PESONet Microservice";
            if (endpoint.Contains("/cards")) return "Cards Microservice";
            if (endpoint.Contains("/bayad")) return "BayadCenter Microservice";
            return "Internal Gateway Service";
        }
    }
}