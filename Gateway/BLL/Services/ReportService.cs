using AutoMapper;
using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Gateway.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text;
using Model = Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services
{
    public class ReportService(
        IConfiguration configuration,
        EFDbContext efDbContext,
        IMapper mapper,
        ILogService logService) : IReportService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly EFDbContext _efDbContext = efDbContext;
        private readonly IMapper _mapper = mapper;
        private readonly ILogService _logService = logService;
        private readonly string _moduleName = "TransactionReport";

        public async Task<List<Response.ReportRouteOption>> GetRoutes()
        {
            try
            {
                return await _efDbContext.Set<Model.Route>()
                    .AsNoTracking()
                    .OrderBy(r => r.Code)
                    .Select(r => new Response.ReportRouteOption
                    {
                        Id = r.Id.ToString(),
                        Code = r.Code,
                        Name = r.Name
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }

        public async Task<List<string>> GetClients()
        {
            try
            {
                return await _efDbContext.Set<Model.HttpLog>()
                    .AsNoTracking()
                    .Where(l => l.ClientId != null && l.ClientId != "")
                    .Select(l => l.ClientId!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }

        public async Task<Response.VTransactionReport> Filter(Request.FParam model)
        {
            try
            {
                string sortCol = string.IsNullOrWhiteSpace(model.SortColumn) ? "RequestDate" : model.SortColumn;
                var propertySelector = EFramework.BuildPropertySelector<Model.HttpLog>(sortCol);

                Response.VTransactionReport vData = new();

                var query = await BuildFilteredHttpLogQuery(model);

                query = model.Descending
                    ? query.OrderByDescending(propertySelector)
                    : query.OrderBy(propertySelector);

                vData.CurrentPage = model.PageNum;
                vData.TotalRecord = await query.CountAsync();
                vData.TotalPage = (int)Math.Ceiling((double)vData.TotalRecord / model.PageSize);

                if (vData.TotalRecord > 0)
                {
                    vData.TotalSuccess2xx = await query
                        .CountAsync(l => l.ResponseCode != null && l.ResponseCode.StartsWith("2"));

                    vData.TotalError4xx5xx = await query
                        .CountAsync(l => l.ResponseCode != null && (l.ResponseCode.StartsWith("4") || l.ResponseCode.StartsWith("5")));

                    var avgDuration = await query
                        .Where(l => l.ResponseDate.HasValue)
                        .Select(l => (double?)EF.Functions.DateDiffMillisecond(l.RequestDate, l.ResponseDate!.Value))
                        .AverageAsync();

                    vData.AverageLatencyMs = Math.Round(avgDuration ?? 0, 2);
                }

                int recordsToSkip = (model.PageNum - 1) * model.PageSize;
                var pagedRows = await query
                    .Skip(recordsToSkip)
                    .Take(model.PageSize)
                    .Select(l => new
                    {
                        l.Id,
                        l.RequestDate,
                        l.ClientId,
                        l.HttpMethod,
                        l.TraceId,
                        l.RouteId,
                        l.Uri,
                        l.ResponseCode,
                        Latency = l.ResponseDate.HasValue
                            ? (double?)EF.Functions.DateDiffMillisecond(l.RequestDate, l.ResponseDate.Value)
                            : 0
                    })
                    .ToListAsync();

                vData.Data = await CorrelateRowsAsync(pagedRows.Select(r => (
                    r.Id,
                    r.RequestDate,
                    r.ClientId,
                    r.HttpMethod,
                    r.TraceId,
                    r.RouteId,
                    r.Uri,
                    r.ResponseCode,
                    r.Latency ?? 0
                )).ToList());

                return vData;
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }

        public async Task<byte[]> ExportCsv(Request.FParam model)
        {
            try
            {
                var user = _efDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };

                string sortCol = string.IsNullOrWhiteSpace(model.SortColumn) ? "RequestDate" : model.SortColumn;
                var propertySelector = EFramework.BuildPropertySelector<Model.HttpLog>(sortCol);

                var query = await BuildFilteredHttpLogQuery(model);

                query = model.Descending
                    ? query.OrderByDescending(propertySelector)
                    : query.OrderBy(propertySelector);

                var rawRows = await query
                    .Take(50000)
                    .Select(l => new
                    {
                        l.Id,
                        l.RequestDate,
                        l.ClientId,
                        l.HttpMethod,
                        l.TraceId,
                        l.RouteId,
                        l.Uri,
                        l.ResponseCode,
                        Latency = l.ResponseDate.HasValue
                            ? (double?)EF.Functions.DateDiffMillisecond(l.RequestDate, l.ResponseDate.Value)
                            : 0
                    })
                    .ToListAsync();

                var items = await CorrelateRowsAsync(rawRows.Select(r => (
                    r.Id,
                    r.RequestDate,
                    r.ClientId,
                    r.HttpMethod,
                    r.TraceId,
                    r.RouteId,
                    r.Uri,
                    r.ResponseCode,
                    r.Latency ?? 0
                )).ToList());

                var sb = new StringBuilder();
                sb.AppendLine("Timestamp,Client ID,Method,Transaction ID,Trace ID,Route,Endpoint URI,Latency (ms),Status Code");

                foreach (var item in items)
                {
                    sb.AppendLine(string.Join(",",
                        EscapeCsv(item.RequestDate.ToString("yyyy-MM-dd HH:mm:ss")),
                        EscapeCsv(item.ClientId ?? ""),
                        EscapeCsv(item.HttpMethod),
                        EscapeCsv(item.TransactionId ?? ""),
                        EscapeCsv(item.TraceId),
                        EscapeCsv(item.RouteName),
                        EscapeCsv(item.Uri),
                        item.LatencyMs.ToString("F2"),
                        EscapeCsv(item.ResponseCode)
                    ));
                }

                _logService.LogActivity(new Model.ActivityLog
                {
                    UserId = user.Id,
                    ModuleName = _moduleName,
                    Action = "EXPORT",
                    Details = $"Exported {items.Count} transaction log records to CSV from terminal [{model.Terminal}]."
                });

                var preamble = Encoding.UTF8.GetPreamble();
                var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
                var result = new byte[preamble.Length + contentBytes.Length];
                Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
                Buffer.BlockCopy(contentBytes, 0, result, preamble.Length, contentBytes.Length);

                return result;
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }

        private async Task<IQueryable<Model.HttpLog>> BuildFilteredHttpLogQuery(Request.FParam model)
        {
            var query = _efDbContext.Set<Model.HttpLog>()
                .AsNoTracking()
                .AsQueryable();

            if (model.Filters == null || model.Filters.Count == 0)
                return query;

            var remainingFilters = new List<Model.Filter>();

            foreach (var f in model.Filters)
            {
                if (string.IsNullOrWhiteSpace(f.Property))
                    continue;

                string valStr = f.Value?.ToString()?.Trim() ?? string.Empty;
                string val2Str = f.Value2?.ToString()?.Trim() ?? string.Empty;
                string op = f.Operator?.ToUpperInvariant() ?? "EQUALS";

                if (f.IsDate && op == "BETWEEN" && f.Property.Equals("RequestDate", StringComparison.OrdinalIgnoreCase))
                {
                    if (DateTime.TryParse(valStr, out var startDt))
                        query = query.Where(l => l.RequestDate >= startDt);

                    if (DateTime.TryParse(val2Str, out var endDt))
                        query = query.Where(l => l.RequestDate <= endDt);
                }
                else if (f.Property.Equals("TransactionId", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(valStr))
                {
                    var matchedTraces = await _efDbContext.Set<Model.TransactionLog>()
                        .AsNoTracking()
                        .Where(t => t.TraceId != null && t.TransactionId != null && t.TransactionId.Contains(valStr))
                        .Select(t => t.TraceId!)
                        .Distinct()
                        .Take(500)
                        .ToListAsync();

                    query = query.Where(l => l.TraceId != null && matchedTraces.Contains(l.TraceId));
                }
                else if (f.Property.Equals("StatusCategory", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(valStr))
                {
                    query = valStr.ToUpperInvariant() switch
                    {
                        "2XX" => query.Where(l => l.ResponseCode != null && l.ResponseCode.StartsWith("2")),
                        "4XX" => query.Where(l => l.ResponseCode != null && l.ResponseCode.StartsWith("4")),
                        "5XX" => query.Where(l => l.ResponseCode != null && l.ResponseCode.StartsWith("5")),
                        _ => query
                    };
                }
                else if (!string.IsNullOrEmpty(valStr) || (f.Values != null && f.Values.Any()))
                {
                    // ClientId, RouteId, TraceId, Uri ay awtomatikong mapoproseso rito
                    remainingFilters.Add(f);
                }
            }

            if (remainingFilters.Count != 0)
            {
                query = query.Where(ExpressionBuilder.GetExpression<Model.HttpLog>(remainingFilters)!);
            }

            return query;
        }

        private async Task<List<Response.FTransactionReport>> CorrelateRowsAsync(
            List<(long Id, DateTime RequestDate, string? ClientId, string? HttpMethod, string? TraceId, string? RouteId, string? Uri, string? ResponseCode, double Latency)> rows)
        {
            if (rows.Count == 0) return [];

            var routeIds = rows
                .Where(r => !string.IsNullOrEmpty(r.RouteId) && long.TryParse(r.RouteId, out _))
                .Select(r => Convert.ToInt64(r.RouteId))
                .Distinct()
                .ToList();

            var routesMap = routeIds.Count > 0
                ? await _efDbContext.Set<Model.Route>()
                    .AsNoTracking()
                    .Where(r => routeIds.Contains(r.Id))
                    .ToDictionaryAsync(r => r.Id.ToString(), r => $"{r.Code} - {r.Name}")
                : [];

            var traceIds = rows
                .Where(r => !string.IsNullOrEmpty(r.TraceId))
                .Select(r => r.TraceId!)
                .Distinct()
                .ToList();

            var tranMap = new Dictionary<string, string>();
            if (traceIds.Count > 0)
            {
                var tranRecords = await _efDbContext.Set<Model.TransactionLog>()
                    .AsNoTracking()
                    .Where(t => t.TraceId != null && traceIds.Contains(t.TraceId) && !string.IsNullOrEmpty(t.TransactionId))
                    .OrderByDescending(t => t.Id)
                    .Select(t => new { TraceId = t.TraceId!, TransactionId = t.TransactionId! })
                    .ToListAsync();

                foreach (var item in tranRecords)
                {
                    tranMap.TryAdd(item.TraceId, item.TransactionId);
                }
            }

            return rows.Select(r => new Response.FTransactionReport
            {
                Id = r.Id,
                RequestDate = r.RequestDate,
                ClientId = r.ClientId,
                HttpMethod = r.HttpMethod ?? "GET",
                TransactionId = (!string.IsNullOrEmpty(r.TraceId) && tranMap.TryGetValue(r.TraceId, out var txn)) ? txn : null,
                TraceId = r.TraceId ?? string.Empty,
                RouteName = (!string.IsNullOrEmpty(r.RouteId) && routesMap.TryGetValue(r.RouteId, out var rName)) ? rName : "Unmapped Route",
                Uri = r.Uri ?? string.Empty,
                LatencyMs = Math.Round(r.Latency, 2),
                ResponseCode = r.ResponseCode ?? "200"
            }).ToList();
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}