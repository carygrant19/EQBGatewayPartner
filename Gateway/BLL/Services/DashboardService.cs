using Gateway.BLL.DTO.Response;
using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Model = Gateway.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Gateway.BLL.Services
{
    public class DashboardService(EFDbContext dbContext, ILogService logService) //: IDashboardService
    {
        private readonly EFDbContext _dbContext = dbContext;
        private readonly ILogService _logService = logService;

        //public async Task<DashboardSummary> GetDashboardMetricsAsync()
        //{
        //    var summary = new DashboardSummary();
        //    var today = DateTime.Today;

        //    try
        //    {
        //        // 1. Basic Gateway Counts
        //        summary.ActiveRoutesCount = await _dbContext.Set<Model.Route>().CountAsync(r => r.IsActive);
        //        summary.ActiveClientsCount = await _dbContext.Set<Client>().CountAsync(c => !c.Deleted && c.Status == "Active");
        //        summary.UnhealthyHostsCount = await _dbContext.Set<TargetHost>().CountAsync(h => !h.IsHealthy);

        //        // 2. HTTP Logs Today Query
        //        var todayLogsQuery = _dbContext.HttpLog
        //            .Where(l => l.RequestDate >= today)
        //            .AsNoTracking();

        //        summary.TotalRequestsToday = await todayLogsQuery.CountAsync();

        //        if (summary.TotalRequestsToday > 0)
        //        {
        //            // Status Code Classification
        //            var logsList = await todayLogsQuery.Select(l => new {
        //                l.ResponseCode,
        //                l.RequestDate,
        //                l.ResponseDate,
        //                l.RouteId,
        //                l.ClientId,
        //                l.TraceId,
        //                l.HttpMethod,
        //                l.Uri
        //            }).ToListAsync();

        //            summary.Status2xxCount = logsList.Count(l => l.ResponseCode != null && l.ResponseCode.StartsWith("2"));
        //            summary.Status4xxCount = logsList.Count(l => l.ResponseCode != null && l.ResponseCode.StartsWith("4"));
        //            summary.Status5xxCount = logsList.Count(l => l.ResponseCode != null && l.ResponseCode.StartsWith("5"));

        //            double totalErrors = summary.Status4xxCount + summary.Status5xxCount;
        //            summary.SuccessRatePercentage = Math.Round(((summary.TotalRequestsToday - totalErrors) / summary.TotalRequestsToday) * 100, 2);

        //            // Average Latency
        //            var latencies = logsList
        //                .Where(l => l.ResponseDate.HasValue)
        //                .Select(l => (l.ResponseDate!.Value - l.RequestDate).TotalMilliseconds);

        //            summary.AverageLatencyMs = latencies.Any() ? Math.Round(latencies.Average(), 2) : 0;

        //            // Hourly Traffic Distribution
        //            summary.HourlyTraffic = logsList
        //                .GroupBy(l => l.RequestDate.Hour)
        //                .Select(g => new HourlyTrafficDto
        //                {
        //                    Hour = $"{g.Key:D2}:00",
        //                    Requests = g.Count(),
        //                    Errors = g.Count(l => l.ResponseCode != null && (l.ResponseCode.StartsWith("4") || l.ResponseCode.StartsWith("5")))
        //                })
        //                .OrderBy(g => g.Hour)
        //                .ToList();

        //            // Recent Logs Activity Feed
        //            summary.RecentLogs = logsList
        //                .OrderByDescending(l => l.RequestDate)
        //                .Take(10)
        //                .Select(l => new RecentAuditLogDto
        //                {
        //                    TraceId = l.TraceId ?? "",
        //                    HttpMethod = l.HttpMethod ?? "GET",
        //                    Uri = l.Uri ?? "",
        //                    ResponseCode = l.ResponseCode ?? "200",
        //                    LatencyMs = l.ResponseDate.HasValue ? Math.Round((l.ResponseDate.Value - l.RequestDate).TotalMilliseconds, 2) : 0,
        //                    RequestDate = l.RequestDate
        //                })
        //                .ToList();
        //        }

        //        // 3. Top Active Routes
        //        var topRouteIds = await _dbContext.HttpLog
        //            .Where(l => l.RequestDate >= today && !string.IsNullOrEmpty(l.RouteId))
        //            .GroupBy(l => l.RouteId)
        //            .Select(g => new { RouteId = g.Key, Count = g.Count() })
        //            .OrderByDescending(g => g.Count)
        //            .Take(5)
        //            .ToListAsync();

        //        var routeIds = topRouteIds.Select(r => Convert.ToInt64(r.RouteId)).ToList();
        //        var routesMap = await _dbContext.Set<Model.Route>()
        //            .Where(r => routeIds.Contains(r.Id))
        //            .ToDictionaryAsync(r => r.Id.ToString(), r => new { r.Code, r.Name });

        //        foreach (var item in topRouteIds)
        //        {
        //            if (item.RouteId != null && routesMap.TryGetValue(item.RouteId, out var routeInfo))
        //            {
        //                summary.TopRoutes.Add(new TopRouteMetricDto
        //                {
        //                    RouteCode = routeInfo.Code,
        //                    RouteName = routeInfo.Name,
        //                    RequestCount = item.Count
        //                });
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logService.LogException(ex, "DashboardService");
        //    }

        //    return summary;
        //}
    }
}