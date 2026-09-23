using AutoMapper;
using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Gateway.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Transactions;
using Model = Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services
{
    public class RouteService : IRouteService
    {
        private readonly EFDbContext _efDbContext;
        private readonly IRepository<Model.Route> _repository;
        private readonly ILogService _logService;
        private readonly IMapper _mapper;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _moduleName = "Route";
        private readonly string _gatewayProxyUrl;

        public RouteService(
            EFDbContext efDbContext,
            IRepository<Model.Route> repository,
            ILogService logService,
            IMapper mapper,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _efDbContext = efDbContext;
            _repository = repository;
            _logService = logService;
            _mapper = mapper;
            _httpClient = httpClient;
            _configuration = configuration;

            _gatewayProxyUrl = _configuration["AppSettings:ProxyUrl"] ?? "https://localhost:7000";
        }

        public async Task<List<Response.Route>> GetActiveEndpointsAsync()
        {
            try
            {
                var endpoints = await _efDbContext.Set<Model.Route>()
                    .Include(e => e.Category)
                    .Include(e => e.AuthProvider)
                    .Include(e => e.OutboundAuthProfile)
                    .Include(e => e.ClientRouteAccess)
                    .Include(e => e.TargetHosts)
                    .Include(e => e.IpRules)
                    .Include(e => e.Transforms)
                    .Where(e => e.IsActive)
                    .AsNoTracking()
                    .ToListAsync();

                var mapped = _mapper.Map<List<Response.Route>>(endpoints);
                for (int i = 0; i < endpoints.Count; i++)
                {
                    mapped[i].ClientIds = endpoints[i].ClientRouteAccess.Select(ra => ra.ClientId).ToList();
                }
                return mapped;
            }
            catch (Exception ex) { _logService.LogException(ex, _moduleName); throw; }
        }

        public async Task<Response.VRoute> FilterAsync(Request.FParam model)
        {
            try
            {
                var propertySelector = EFramework.BuildPropertySelector<Model.Route>(model.SortColumn);
                Response.VRoute vData = new();

                var query = _efDbContext.Set<Model.Route>()
                    .Include(e => e.Category)
                    .Include(e => e.AuthProvider)
                    .Include(e => e.OutboundAuthProfile)
                    .Include(e => e.ClientRouteAccess)
                    .Include(e => e.TargetHosts)
                    .Include(e => e.IpRules)
                    .Include(e => e.Transforms)
                    .AsQueryable();

                if (model.Filters != null && model.Filters.Count != 0)
                {
                    string filterValue = Convert.ToString(model.Filters[0].Value) ?? string.Empty;
                    query = query.Where(u => u.Name.Contains(filterValue)
                                          || u.UpstreamPathTemplate.Contains(filterValue)
                                          || (u.Category != null && u.Category.Name.Contains(filterValue)));
                }

                query = model.Descending ? query.OrderByDescending(propertySelector) : query.OrderBy(propertySelector);

                vData.CurrentPage = model.PageNum;
                vData.TotalRecord = await query.CountAsync();
                vData.TotalPage = (int)Math.Ceiling((double)vData.TotalRecord / model.PageSize);

                int recordsToSkip = (model.PageNum - 1) * model.PageSize;
                var pagedQuery = await query.Skip(recordsToSkip).Take(model.PageSize).ToListAsync();

                vData.Data = _mapper.Map<List<Response.FRoute>>(pagedQuery);

                for (int i = 0; i < pagedQuery.Count; i++)
                {
                    vData.Data[i].ClientIds = pagedQuery[i].ClientRouteAccess.Select(ra => ra.ClientId).ToList();
                }

                return vData;
            }
            catch (Exception ex) { _logService.LogException(ex, _moduleName); throw; }
        }

        public async Task<Response.Result> CreateAsync(Request.Route model)
        {
            Response.Result result = new();
            try
            {
                bool isSuccess = false;
                using (TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled))
                {
                    var user = _efDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };
                    var exists = await _efDbContext.Set<Model.Route>().AnyAsync(d => d.Code == model.Code);

                    if (!exists)
                    {
                        var data = _mapper.Map<Model.Route>(model);
                        data.RequireApiKey = model.RequireApiKey;
                        data.OutboundAuthProfileId = model.OutboundAuthProfileId;
                        data.UpstreamPathTemplate = NormalizePathTemplate(data.UpstreamPathTemplate);
                        data.DownstreamPathTemplate = NormalizePathTemplate(data.DownstreamPathTemplate);

                        data.TargetHosts = new List<Model.TargetHost>();
                        data.IpRules = new List<Model.RouteIpRule>();
                        data.Transforms = new List<Model.RouteTransform>();

                        data.CreatedBy = (int)user.Id;
                        data.CreatedDate = DateTime.Now;
                        data.IsActive = true;

                        await _repository.AddAsync(data);
                        await _efDbContext.SaveChangesAsync();
                        await SyncClientRouteAccessAsync(data.Id, model.ClientIds);

                        if (model.TargetHosts != null && model.TargetHosts.Count > 0)
                        {
                            foreach (var h in model.TargetHosts)
                                data.TargetHosts.Add(new Model.TargetHost { Host = h.Host, Port = h.Port, Weight = h.Weight, Description = h.Description, HealthCheckPath = h.HealthCheckPath, IsHealthy = h.IsHealthy });
                        }

                        if (model.IpRules != null && model.IpRules.Count > 0)
                        {
                            foreach (var i in model.IpRules)
                                data.IpRules.Add(new Model.RouteIpRule { IpAddressOrRange = i.IpAddressOrRange, RuleType = i.RuleType, Description = i.Description });
                        }

                        if (model.Transforms != null && model.Transforms.Count > 0)
                        {
                            foreach (var t in model.Transforms)
                                data.Transforms.Add(new Model.RouteTransform { TransformPhase = t.TransformPhase, Action = t.Action, HeaderName = t.HeaderName, HeaderValue = t.HeaderValue });
                        }

                        await _efDbContext.SaveChangesAsync();
                        _logService.LogActivity(new Model.ActivityLog { UserId = user.Id, ModuleName = _moduleName, Action = "ADD", Details = $"[{data.Name}] proxy path registered (Draft)." });

                        transactionScope.Complete();
                        isSuccess = true;
                    }
                }

                if (isSuccess)
                    result = new Response.Result { Status = "SUCCESS", Message = $"{_moduleName} saved in DB. Remember to PUBLISH changes." };
                else
                    result = new Response.Result { Status = "FAILED", Message = $"{_moduleName} endpoint code already exists." };
            }
            catch (Exception ex) { _logService.LogException(ex, _moduleName); throw; }
            return result;
        }

        public async Task<Response.Result> UpdateAsync(Request.Route model)
        {
            Response.Result result = new();
            try
            {
                bool isSuccess = false; bool isNotFound = false;
                using (TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled))
                {
                    var user = await _efDbContext.User!.FirstOrDefaultAsync(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };

                    var data = await _efDbContext.Set<Model.Route>()
                        .Include(e => e.OutboundAuthProfile)
                        .Include(e => e.TargetHosts)
                        .Include(e => e.IpRules)
                        .Include(e => e.Transforms)
                        .FirstOrDefaultAsync(l => l.Id == Convert.ToInt64(model.Id));

                    if (data != null)
                    {
                        _efDbContext.Set<Model.TargetHost>().RemoveRange(data.TargetHosts);
                        _efDbContext.Set<Model.RouteIpRule>().RemoveRange(data.IpRules);
                        _efDbContext.Set<Model.RouteTransform>().RemoveRange(data.Transforms);
                        data.TargetHosts.Clear(); data.IpRules.Clear(); data.Transforms.Clear();

                        var incomingHosts = model.TargetHosts;
                        var incomingIpRules = model.IpRules;
                        var incomingTransforms = model.Transforms;

                        model.TargetHosts = new(); model.IpRules = new(); model.Transforms = new();

                        _mapper.Map(model, data);
                        data.RequireApiKey = model.RequireApiKey;
                        data.OutboundAuthProfileId = model.OutboundAuthProfileId;
                        data.UpstreamPathTemplate = NormalizePathTemplate(data.UpstreamPathTemplate);
                        data.DownstreamPathTemplate = NormalizePathTemplate(data.DownstreamPathTemplate);

                        data.UpdatedBy = (int)user.Id;
                        data.UpdatedDate = DateTime.Now;

                        await _repository.UpdateAsync(data);
                        await SyncClientRouteAccessAsync(data.Id, model.ClientIds);

                        if (incomingHosts != null && incomingHosts.Count > 0)
                        {
                            foreach (var h in incomingHosts)
                                data.TargetHosts.Add(new Model.TargetHost { RouteId = data.Id, Host = h.Host, Port = h.Port, Weight = h.Weight, Description = h.Description, HealthCheckPath = h.HealthCheckPath, IsHealthy = h.IsHealthy });
                        }

                        if (incomingIpRules != null && incomingIpRules.Count > 0)
                        {
                            foreach (var i in incomingIpRules)
                                data.IpRules.Add(new Model.RouteIpRule { RouteId = data.Id, IpAddressOrRange = i.IpAddressOrRange, RuleType = i.RuleType, Description = i.Description });
                        }

                        if (incomingTransforms != null && incomingTransforms.Count > 0)
                        {
                            foreach (var t in incomingTransforms)
                                data.Transforms.Add(new Model.RouteTransform { RouteId = data.Id, TransformPhase = t.TransformPhase, Action = t.Action, HeaderName = t.HeaderName, HeaderValue = t.HeaderValue });
                        }

                        await _efDbContext.SaveChangesAsync();
                        _logService.LogActivity(new Model.ActivityLog { UserId = user.Id, ModuleName = _moduleName, Action = "EDIT", Details = $"[{data.Name}] definition updated (Draft)." });

                        transactionScope.Complete();
                        isSuccess = true;
                    }
                    else { isNotFound = true; }
                }

                if (isSuccess)
                {
                    result.Status = "SUCCESS";
                    result.Message = $"{_moduleName} updated in DB. Remember to PUBLISH changes.";
                }
                else if (isNotFound)
                {
                    result.Status = "FAILED";
                    result.Message = $"{_moduleName} target endpoint does not exist.";
                }
            }
            catch (Exception ex) { _logService.LogException(ex, _moduleName); throw; }
            return result;
        }

        public async Task<Response.Result> DeleteAsync(Request.Route model)
        {
            Response.Result result = new();
            try
            {
                bool isSuccess = false; bool isNotFound = false;
                using (TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled))
                {
                    var user = _efDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };
                    var data = await _efDbContext.Set<Model.Route>().FirstOrDefaultAsync(l => l.Id == Convert.ToInt64(model.Id));

                    if (data != null)
                    {
                        data.IsActive = false;
                        data.UpdatedBy = (int)user.Id;
                        data.UpdatedDate = DateTime.Now;

                        await _repository.UpdateAsync(data);
                        await _efDbContext.SaveChangesAsync();
                        _logService.LogActivity(new Model.ActivityLog { UserId = user.Id, ModuleName = _moduleName, Action = "DELETE", Details = $"[{data.Name}] gateway entry disabled (Draft)." });

                        transactionScope.Complete();
                        isSuccess = true;
                    }
                    else { isNotFound = true; }
                }

                if (isSuccess)
                {
                    result.Status = "SUCCESS";
                    result.Message = $"{_moduleName} disabled in DB. Remember to PUBLISH changes.";
                }
                else if (isNotFound)
                {
                    result.Status = "FAILED";
                    result.Message = $"{_moduleName} reference endpoint does not exist.";
                }
            }
            catch (Exception ex) { _logService.LogException(ex, _moduleName); throw; }
            return result;
        }

        public async Task<Response.Result> RestoreAsync(Request.Route model)
        {
            Response.Result result = new();
            try
            {
                bool isSuccess = false; bool isNotFound = false;
                using (TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled))
                {
                    var user = _efDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };
                    var data = await _efDbContext.Set<Model.Route>().FirstOrDefaultAsync(l => l.Id == Convert.ToInt64(model.Id));

                    if (data != null)
                    {
                        data.IsActive = true;
                        data.UpdatedBy = (int)user.Id;
                        data.UpdatedDate = DateTime.Now;

                        await _repository.UpdateAsync(data);
                        await _efDbContext.SaveChangesAsync();
                        _logService.LogActivity(new Model.ActivityLog { UserId = user.Id, ModuleName = _moduleName, Action = "RESTORE", Details = $"[{data.Name}] proxy path restored (Draft)." });

                        transactionScope.Complete();
                        isSuccess = true;
                    }
                    else { isNotFound = true; }
                }

                if (isSuccess)
                {
                    result.Status = "SUCCESS";
                    result.Message = $"{_moduleName} reactivated in DB. Remember to PUBLISH changes.";
                }
                else if (isNotFound)
                {
                    result.Status = "FAILED";
                    result.Message = $"{_moduleName} targeted endpoint missing.";
                }
            }
            catch (Exception ex) { _logService.LogException(ex, _moduleName); throw; }
            return result;
        }

        // ====================================================================
        // PUBLISH METHOD WITH YARP OFFICIAL FORMAT & YEAR/MONTH BACKUP FOLDERS
        // ====================================================================
        public async Task<Response.Result> PublishRoutesAsync(string opUser)
        {
            Response.Result result = new();
            try
            {
                DateTime now = DateTime.Now;
                var user = await _efDbContext.User!.FirstOrDefaultAsync(d => d.Username == opUser) ?? new Model.User() { Id = 0 };

                // 1. Kuhanin ang buong gateway configuration mula sa DB
                var activeRoutes = await GetActiveEndpointsAsync();
                var authProviders = await _efDbContext.Set<Model.AuthProvider>().AsNoTracking().ToListAsync();
                var outboundProfiles = await _efDbContext.Set<Model.OutboundAuthProfile>()
                    .Include(p => p.Headers)
                    .AsNoTracking()
                    .ToListAsync();
                var categories = await _efDbContext.Set<Model.Category>().AsNoTracking().ToListAsync();

                // 2. I-build ang YARP Official format (Routes at Clusters)
                var yarpRoutes = new Dictionary<string, object>();
                var yarpClusters = new Dictionary<string, object>();

                foreach (var r in activeRoutes)
                {
                    var methods = string.IsNullOrWhiteSpace(r.AllowedMethods)
                        ? null
                        : r.AllowedMethods.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(m => m.Trim().ToUpper())
                                          .ToArray();

                    yarpRoutes[r.Code] = new
                    {
                        ClusterId = r.Code,
                        Match = new
                        {
                            Path = r.UpstreamPathTemplate,
                            Methods = methods
                        }
                    };

                    var destinations = new Dictionary<string, object>();
                    if (r.TargetHosts != null && r.TargetHosts.Count > 0)
                    {
                        for (int i = 0; i < r.TargetHosts.Count; i++)
                        {
                            var host = r.TargetHosts[i];
                            string addressUrl = $"{r.DownstreamScheme}://{host.Host}";
                            if (host.Port > 0) addressUrl += $":{host.Port}";
                            addressUrl += r.DownstreamPathTemplate;

                            destinations[$"destination_{i + 1}"] = new { Address = addressUrl };
                        }
                    }

                    yarpClusters[r.Code] = new
                    {
                        Destinations = destinations
                    };
                }

                // 3. I-compile ang Final YARP Snapshot
                var fullYarpConfigFile = new
                {
                    PublishedDate = now,
                    PublishedBy = opUser,
                    ReverseProxy = new
                    {
                        Routes = yarpRoutes,
                        Clusters = yarpClusters
                    },
                    Metadata = new
                    {
                        RawRoutes = activeRoutes,
                        AuthProviders = authProviders,
                        OutboundAuthProfiles = outboundProfiles,
                        Categories = categories
                    }
                };

                var newConfigJson = JsonConvert.SerializeObject(fullYarpConfigFile, Formatting.Indented, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

                // 4. FILE PATHS & YEAR/MONTH FOLDER CREATION
                string filePath = _configuration["YarpConfigFile:FilePath"] ?? @"C:\Gateway\gateway-config.json";
                string baseBackupPath = _configuration["YarpConfigFile:BackupPath"] ?? @"C:\Gateway\YARPBackup";

                // Save Main Gateway File
                string? fileDir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
                {
                    Directory.CreateDirectory(fileDir);
                }
                await File.WriteAllTextAsync(filePath, newConfigJson);

                // Dynamically build: YARPBackup / YYYY / MM
                string yearFolder = now.ToString("yyyy");
                string monthFolder = now.ToString("MM");
                string targetBackupDir = Path.Combine(baseBackupPath, yearFolder, monthFolder);

                if (!Directory.Exists(targetBackupDir))
                {
                    Directory.CreateDirectory(targetBackupDir);
                }

                string timestamp = now.ToString("yyyyMMdd_HHmmss");
                string backupFilePath = Path.Combine(targetBackupDir, $"yarp-config_{timestamp}.json");
                await File.WriteAllTextAsync(backupFilePath, newConfigJson);

                // 5. Save Audit Log
                var lastPublishLog = await _efDbContext.Set<Model.AuditLog>()
                    .Where(a => a.RecordId == "PROXY_CONFIG" && a.OperationType == "PUBLISH")
                    .OrderByDescending(a => a.ActionDate)
                    .FirstOrDefaultAsync();

                string previousConfigJson = lastPublishLog?.NewData ?? "[]";

                var auditLog = new Model.AuditLog
                {
                    RecordId = "PROXY_CONFIG",
                    Terminal = "SYSTEM",
                    OperationType = "PUBLISH",
                    ChangeBy = user.Id,
                    ActionDate = now,
                    TableName = "Master_ApiEndpoint",
                    OriginalData = previousConfigJson,
                    NewData = newConfigJson
                };

                _logService.LogAudit(auditLog);
                _logService.LogActivity(new Model.ActivityLog { UserId = user.Id, ModuleName = _moduleName, Action = "PUBLISH", Details = $"All gateway proxy routes published. Backup saved to: {yearFolder}/{monthFolder}/{Path.GetFileName(backupFilePath)}" });

                // 6. Signal Gateway Proxy Reload
                var reloadUrl = $"{_gatewayProxyUrl.TrimEnd('/')}/internal/gateway/reload";
                var response = await _httpClient.PostAsync(reloadUrl, null);

                if (response.IsSuccessStatusCode)
                {
                    result.Status = "SUCCESS";
                    result.Message = "Gateway Proxy configuration successfully published, backed up by Year/Month, and live!";
                }
                else
                {
                    result.Status = "FAILED";
                    result.Message = $"File saved & backed up, but failed to signal Gateway Proxy reload ({response.StatusCode}).";
                }
            }
            catch (Exception ex) { _logService.LogException(ex, _moduleName); throw; }

            return result;
        }

        private async Task SyncClientRouteAccessAsync(long routeId, List<int> clientIds)
        {
            var existingAccess = await _efDbContext.Set<Model.ClientRouteAccess>()
                .Where(ra => ra.RouteId == routeId)
                .ToListAsync();

            _efDbContext.Set<Model.ClientRouteAccess>().RemoveRange(existingAccess);

            if (clientIds != null && clientIds.Count > 0)
            {
                foreach (var clientId in clientIds)
                {
                    _efDbContext.Set<Model.ClientRouteAccess>().Add(new Model.ClientRouteAccess
                    {
                        RouteId = routeId,
                        ClientId = clientId,
                        IsAllowed = true,
                        CreatedDate = DateTime.Now
                    });
                }
            }

            await _efDbContext.SaveChangesAsync();
        }

        private static string NormalizePathTemplate(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            var trimmed = path.Trim();
            while (trimmed.Contains("//")) trimmed = trimmed.Replace("//", "/");
            if (!trimmed.StartsWith("/")) trimmed = "/" + trimmed;
            return trimmed;
        }
    }
}