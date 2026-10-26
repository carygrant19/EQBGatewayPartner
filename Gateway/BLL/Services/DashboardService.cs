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

                    // Average Latency
                    var avgDuration = await todayLogsQuery
                        .Where(l => l.ResponseDate.HasValue)
                        .Select(l => (double?)EF.Functions.DateDiffMillisecond(l.RequestDate, l.ResponseDate!.Value))
                        .AverageAsync(cancellationToken);

                    summary.AverageLatencyMs = Math.Round(avgDuration ?? 0, 2);

                    // Hourly Traffic Grouping
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

                    // Top 5 Endpoints
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

                    // Recent 10 HTTP Logs
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

                    // I-correlate ang TransactionId mula sa Log_Transaction gamit ang TraceId
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

                // 3. Recent 5 Exceptions mula sa Log_Exception gamit ang Model.ExceptionLog
                summary.RecentExceptions = await _dbContext.Set<Model.ExceptionLog>()
                    .OrderByDescending(e => e.LogDate)
                    .Take(5)
                    .Select(e => new RecentExceptionDto
                    {
                        Id = e.Id,
                        ModuleName = e.ModuleName ?? "UNKNOWN",
                        Message = e.Message ?? string.Empty,
                        Time = e.LogDate.ToString("HH:mm:ss")
                    })
                    .ToListAsync(cancellationToken);
            }
            catch
            {
            }

            return summary;
        }
    }
}