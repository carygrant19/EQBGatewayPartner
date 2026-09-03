using AutoMapper;
using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Gateway.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Transactions;
using Model = Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services
{
    public class RouteService(
        IConfiguration configuration,
        EFDbContext efDbContext,
        IMapper mapper,
        IRepository<Model.Route> repository,
        ILogService logService) : IRouteService
    {
        private readonly EFDbContext _efDbContext = efDbContext;
        private readonly IRepository<Model.Route> _repository = repository;
        private readonly ILogService _logService = logService;
        private readonly IMapper _mapper = mapper;
        private readonly string _moduleName = "Route";

        public async Task<Response.Result> GenerateOcelotConfigFile(OcelotConfigFile ocelotConfigFile)
        {
            Response.Result result = new();

            try
            {
                OcelotCustomFileConfiguration config = await Config();

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var configJson = System.Text.Json.JsonSerializer.Serialize(config, options);

                string originalFilePath = ocelotConfigFile.FilePath!;
                string dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
                string fullBackupPath = Path.Combine(ocelotConfigFile.BackupPath!, dateFolder);
                string backupFileName = $"ocelot_{DateTime.Now.ToString("HHmmssf")}.json";

                if (!Directory.Exists(fullBackupPath))
                {
                    Directory.CreateDirectory(fullBackupPath);
                }

                if (File.Exists(originalFilePath))
                {
                    string backupFileFullPath = Path.Combine(fullBackupPath, backupFileName);
                    File.Copy(originalFilePath, backupFileFullPath, overwrite: true);
                }

                await File.WriteAllTextAsync(originalFilePath, configJson, Encoding.UTF8);

                result.Status = "SUCCESS";
                result.Message = "Ocelot gateway custom configuration file compiled successfully.";
            }
            catch (Exception ex)
            {
                result.Status = "FAILED";
                result.Message = $"An error occurred while compiling file configuration: {ex.Message}";
            }

            return result;
        }

        public async Task<OcelotCustomFileConfiguration> Config()
        {
            try
            {
                var result = await _efDbContext.Set<Model.Route>()
                    .Include(c => c.Clients)
                    .Include(i => i.IpRules)
                    .Include(h => h.Hosts)
                    .Where(e => e.IsActive == true)
                    .ToListAsync();

                OcelotCustomFileConfiguration ocelot = new()
                {
                    GlobalConfiguration = new CustomFileGlobalConfiguration
                    {
                        RateLimitOptions = new FileGlobalRateLimitByHeaderRule
                        {
                            ClientIdHeader = "X-Client-Id",
                            QuotaExceededMessage = "API allocation bounds breached.",
                            HttpStatusCode = 429,
                            EnableHeaders = true
                        },
                        LogLevel = "Warning",
                        EnableRequestId = true,
                        AuthenticationOptions = null
                    },
                    Routes = new List<CustomFileRoute>()
                };

                foreach (var item in result)
                {
                    CustomFileRoute oRoute = new()
                    {
                        DownstreamPathTemplate = item.DownstreamPathTemplate,
                        DownstreamScheme = item.DownstreamScheme,
                        UpstreamPathTemplate = item.UpstreamPathTemplate,
                        UpstreamHttpMethod = OcelotMethod(item.UpstreamHttpMethod) ?? new HashSet<string>(),
                        DownstreamHostAndPorts = OcelotHost(item.Hosts) ?? new List<FileHostAndPort>(),
                        SecurityOptions = OcelotSecurityOptions(item.IpRules),
                        RateLimitOptions = OcelotRateLimit(
                                item.EnableRateLimiting,
                                item.RatePeriod,
                                item.RatePeriodTimespan ?? 0,
                                item.RateLimit ?? 0,
                                item.RateLimitHttpStatusCode,
                                item.RateLimitQuotaExceededMessage
                            ),
                        Id = item.Id.ToString(),
                        RequireSignature = item.RequireSignature,
                        Client = OcelotClient(item.Clients),
                        TimeLimit = OcelotTimeLimit(item.EnableTimeLimit, item.TimeFrom, item.TimeTo, item.AllowedDays)
                    };

                    // Direct per-endpoint auth setting based on UI/database input
                    if (!string.IsNullOrWhiteSpace(item.AuthenticationProviderKey))
                    {
                        oRoute.AuthenticationOptions = new FileAuthenticationOptions
                        {
                            AuthenticationProviderKey = item.AuthenticationProviderKey.Trim(),
                            AuthenticationProviderKeys = new string[] { item.AuthenticationProviderKey.Trim() },
                            AllowedScopes = new List<string>()
                        };
                    }
                    else
                    {
                        oRoute.AuthenticationOptions = null;
                    }

                    ocelot.Routes.Add(oRoute);
                }

                return ocelot;
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }

        private static HashSet<string>? OcelotMethod(string httpMethod)
        {
            return string.IsNullOrEmpty(httpMethod)
                ? null
                : httpMethod.Split('|', StringSplitOptions.RemoveEmptyEntries)
                            .Select(m => m.Trim().ToUpper())
                            .ToHashSet();
        }

        private static FileRateLimitByHeaderRule OcelotRateLimit(
             bool enableRateLimiting,
             string? period,
             int timeSpan,
             int limit,
             int? httpStatusCode,
             string? quotaMessage)
        {
            return new FileRateLimitByHeaderRule
            {
                EnableRateLimiting = enableRateLimiting,
                Period = enableRateLimiting ? period : null,
                PeriodTimespan = enableRateLimiting ? (timeSpan > 0 ? timeSpan : 60) : 0,
                Wait = enableRateLimiting ? $"{timeSpan}s" : null,
                Limit = enableRateLimiting ? limit : 0,
                StatusCode = httpStatusCode ?? 429,
                QuotaMessage = quotaMessage,
                EnableHeaders = true,
                ClientWhitelist = new List<string>()
            };
        }

        private static List<FileHostAndPort>? OcelotHost(ICollection<RouteHost> hosts)
        {
            if (hosts == null || !hosts.Any()) return null;

            return hosts.Select(h => new FileHostAndPort
            {
                Host = h.Host,
                Port = h.Port
            }).ToList();
        }

        private static FileSecurityOptions? OcelotSecurityOptions(ICollection<RouteIpRule> ipRules)
        {
            if (ipRules == null || !ipRules.Any()) return null;

            return new FileSecurityOptions
            {
                IPAllowedList = ipRules.Where(r => r.RuleType == "Allow").Select(r => r.IpAddressOrRange).ToList(),
                IPBlockedList = ipRules.Where(r => r.RuleType == "Deny").Select(r => r.IpAddressOrRange).ToList()
            };
        }

        private static List<string> OcelotClient(ICollection<RouteClient> clients)
        {
            return clients != null && clients.Any()
                ? clients.Select(c => c.ClientId).ToList()
                : new List<string>();
        }

        private static Gateway.Data.Models.TimeLimit? OcelotTimeLimit(bool enableTimeLimit, string? timeFrom, string? timeTo, string? allowedDaysString)
        {
            if (!enableTimeLimit) return null;

            List<int> parsedDays = new();
            if (!string.IsNullOrEmpty(allowedDaysString))
            {
                parsedDays = allowedDaysString.Split(',')
                    .Select(d => int.TryParse(d.Trim(), out int val) ? val : -1)
                    .Where(val => val >= 0 && val <= 6)
                    .ToList();
            }

            return new Gateway.Data.Models.TimeLimit
            {
                EnableTimeLimit = enableTimeLimit,
                TimeFrom = timeFrom,
                TimeTo = timeTo,
                AllowedDays = parsedDays
            };
        }

        public async Task<List<Response.Route>> Get()
        {
            try
            {
                var routes = await _efDbContext.Set<Model.Route>()
                    .Include(r => r.Hosts)
                    .Include(r => r.IpRules)
                    .Where(e => e.IsActive == true)
                    .ToListAsync();

                var result = routes.Select(q => new Response.Route
                {
                    Id = q.Id.ToString(),
                    Code = q.Code,
                    Name = q.Name,
                    Description = q.Description,
                    IsActive = q.IsActive,
                    IsWebSocket = q.IsWebSocket,
                    UpstreamPathTemplate = q.UpstreamPathTemplate,
                    UpstreamHttpMethod = q.UpstreamHttpMethod,
                    DownstreamPathTemplate = q.DownstreamPathTemplate,
                    DownstreamScheme = q.DownstreamScheme,
                    UpstreamHost = q.UpstreamHost,
                    DownstreamHttpVersion = q.DownstreamHttpVersion,
                    DangerousAcceptAnyServerCertificateValidator = q.DangerousAcceptAnyServerCertificateValidator,
                    AuthenticationProviderKey = q.AuthenticationProviderKey,
                    RouteIsCaseSensitive = q.RouteIsCaseSensitive,
                    Priority = q.Priority,
                    EnableRateLimiting = q.EnableRateLimiting,
                    RateLimit = q.RateLimit,
                    RatePeriod = q.RatePeriod,
                    RatePeriodTimespan = q.RatePeriodTimespan,
                    RateLimitHttpStatusCode = q.RateLimitHttpStatusCode,
                    RateLimitQuotaExceededMessage = q.RateLimitQuotaExceededMessage,
                    EnableCaching = q.EnableCaching,
                    CacheTtlSeconds = q.CacheTtlSeconds,
                    EnableQoS = q.EnableQoS,
                    QoSTimeoutMs = q.QoSTimeoutMs,
                    QoSExceptionsAllowedBeforeBreaking = q.QoSExceptionsAllowedBeforeBreaking,
                    QoSDurationOfBreakMs = q.QoSDurationOfBreakMs,
                    LoadBalancerType = q.LoadBalancerType,
                    LoadBalancerKey = q.LoadBalancerKey,
                    LoadBalancerExpiryMs = q.LoadBalancerExpiryMs,
                    RequireSignature = q.RequireSignature,
                    EnableTimeLimit = q.EnableTimeLimit,
                    TimeFrom = q.TimeFrom,
                    TimeTo = q.TimeTo,
                    AllowedDays = q.AllowedDays,
                    UseServiceDiscovery = q.UseServiceDiscovery,
                    ServiceName = q.ServiceName,
                    ServiceNamespace = q.ServiceNamespace,
                    EnableServicePolling = q.EnableServicePolling,
                    PollingIntervalMs = q.PollingIntervalMs
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }

        public async Task<Response.VRoute> Filter(Request.FParam model)
        {
            try
            {
                var propertySelector = EFramework.BuildPropertySelector<Model.Route>(model.SortColumn);
                Response.VRoute vData = new();

                var query = _efDbContext.Set<Model.Route>()!
                    .Include(r => r.Hosts)
                    .Include(r => r.IpRules)
                    .Include(r => r.Clients)
                    .AsQueryable();

                if (model.Filters != null && model.Filters.Count != 0)
                {
                    query = query.Where(u => u.Name.Contains(Convert.ToString(model.Filters[0].Value)) || u.UpstreamPathTemplate.Contains(Convert.ToString(model.Filters[0].Value)));
                }

                query = model.Descending
                    ? query.OrderByDescending(propertySelector)
                    : query.OrderBy(propertySelector);

                vData.CurrentPage = model.PageNum;
                vData.TotalRecord = await query.CountAsync();
                vData.TotalPage = (int)Math.Ceiling((double)vData.TotalRecord / model.PageSize);

                int recordsToSkip = (model.PageNum - 1) * model.PageSize;
                var pagedQuery = await query.Skip(recordsToSkip).Take(model.PageSize).ToListAsync();

                vData.Data = pagedQuery.Select(r => new Response.FRoute
                {
                    Id = r.Id.ToString(),
                    Code = r.Code,
                    Name = r.Name,
                    Description = r.Description,
                    Category = r.Category,
                    IsActive = r.IsActive,
                    IsWebSocket = r.IsWebSocket,
                    UpstreamPathTemplate = r.UpstreamPathTemplate,
                    UpstreamHttpMethod = r.UpstreamHttpMethod,
                    DownstreamPathTemplate = r.DownstreamPathTemplate,
                    DownstreamScheme = r.DownstreamScheme,
                    UpstreamHost = r.UpstreamHost,
                    DownstreamHttpVersion = r.DownstreamHttpVersion,
                    DangerousAcceptAnyServerCertificateValidator = r.DangerousAcceptAnyServerCertificateValidator,
                    AuthenticationProviderKey = r.AuthenticationProviderKey,
                    RouteIsCaseSensitive = r.RouteIsCaseSensitive,
                    Priority = r.Priority,

                    EnableRateLimiting = r.EnableRateLimiting,
                    RateLimit = r.RateLimit,
                    RatePeriod = r.RatePeriod,
                    RatePeriodTimespan = r.RatePeriodTimespan,
                    RateLimitHttpStatusCode = r.RateLimitHttpStatusCode,
                    RateLimitQuotaExceededMessage = r.RateLimitQuotaExceededMessage,

                    EnableCaching = r.EnableCaching,
                    CacheTtlSeconds = r.CacheTtlSeconds,
                    EnableQoS = r.EnableQoS,
                    QoSTimeoutMs = r.QoSTimeoutMs,
                    QoSExceptionsAllowedBeforeBreaking = r.QoSExceptionsAllowedBeforeBreaking,
                    QoSDurationOfBreakMs = r.QoSDurationOfBreakMs,

                    LoadBalancerType = r.LoadBalancerType,
                    LoadBalancerKey = r.LoadBalancerKey,
                    LoadBalancerExpiryMs = r.LoadBalancerExpiryMs,
                    RequireSignature = r.RequireSignature,
                    EnableTimeLimit = r.EnableTimeLimit,
                    TimeFrom = r.TimeFrom,
                    TimeTo = r.TimeTo,
                    AllowedDays = r.AllowedDays,

                    UseServiceDiscovery = r.UseServiceDiscovery,
                    ServiceName = r.ServiceName,
                    ServiceNamespace = r.ServiceNamespace,
                    EnableServicePolling = r.EnableServicePolling,
                    PollingIntervalMs = r.PollingIntervalMs,

                    CreatedBy = r.CreatedBy.ToString(),
                    CreatedDate = r.CreatedDate,
                    UpdatedBy = r.UpdatedBy?.ToString(),
                    UpdatedDate = r.UpdatedDate,

                    Hosts = r.Hosts?.Select(h => new Response.FRouteHost
                    {
                        Id = h.Id,
                        RouteId = h.RouteId.ToString(),
                        Host = h.Host,
                        Port = h.Port,
                        Description = h.Description ?? string.Empty
                    }).ToList() ?? new List<Response.FRouteHost>(),

                    IpRules = r.IpRules?.Select(ip => new Response.FRouteIpRule
                    {
                        Id = ip.Id,
                        RouteId = ip.RouteId.ToString(),
                        IpAddressOrRange = ip.IpAddressOrRange,
                        RuleType = ip.RuleType,
                        Description = ip.Description ?? string.Empty
                    }).ToList() ?? new List<Response.FRouteIpRule>(),

                    Clients = r.Clients?.Select(c => new Response.FRouteClient
                    {
                        Id = c.Id,
                        RouteId = c.RouteId.ToString(),
                        ClientId = c.ClientId
                    }).ToList() ?? new List<Response.FRouteClient>()
                }).ToList();

                return vData;
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }

        public async Task<Response.Result> Create(Request.Route model)
        {
            Response.Result result = new();
            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "ADD";
                var user = _efDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };

                var activityLog = new Model.ActivityLog()
                {
                    UserId = user.Id!,
                    ModuleName = _moduleName,
                    Action = action
                };

                var data = await _efDbContext.Set<Model.Route>().FirstOrDefaultAsync(d => d.Code == model.Code);

                if (data == null)
                {
                    data = _mapper.Map<Model.Route>(model);

                    data.Hosts = new List<Model.RouteHost>();
                    data.IpRules = new List<Model.RouteIpRule>();
                    data.Clients = new List<Model.RouteClient>();

                    data.CreatedBy = (int)user.Id;
                    data.CreatedDate = DateTime.Now;
                    data.IsActive = true;

                    await _repository.AddAsync(data);

                    if (model.Hosts != null && model.Hosts.Any())
                    {
                        foreach (var h in model.Hosts)
                        {
                            data.Hosts.Add(new Model.RouteHost
                            {
                                Host = h.Host,
                                Port = h.Port,
                                Description = h.Description
                            });
                        }
                    }

                    if (model.IpRules != null && model.IpRules.Any())
                    {
                        foreach (var i in model.IpRules)
                        {
                            data.IpRules.Add(new Model.RouteIpRule
                            {
                                IpAddressOrRange = i.IpAddressOrRange,
                                RuleType = i.RuleType,
                                Description = i.Description
                            });
                        }
                    }

                    if (model.Clients != null && model.Clients.Any())
                    {
                        foreach (var c in model.Clients)
                        {
                            data.Clients.Add(new Model.RouteClient
                            {
                                ClientId = c.ClientId
                            });
                        }
                    }

                    await _efDbContext.SaveChangesAsync();

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = action,
                        ChangeBy = user.Id!,
                        ActionDate = DateTime.Now,
                        TableName = "Master_Routes",
                        OriginalData = "",
                        NewData = JsonConvert.SerializeObject(data, new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        })
                    };

                    activityLog.Details = string.Format("[{0}] proxy path registered.", data.Name);
                    result = new Response.Result() { Status = "SUCCESS", Message = string.Format("{0} registered successfully.", _moduleName) };

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(auditLog);
                }
                else
                {
                    result = new Response.Result() { Status = "FAILED", Message = string.Format("{0} route code already exists.", _moduleName) };
                }

                transactionScope.Complete();
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
            finally
            {
                transactionScope.Dispose();
            }

            return result;
        }

        public async Task<Response.Result> Update(Request.Route model)
        {
            Response.Result result = new();
            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var user = await _efDbContext.User!.FirstOrDefaultAsync(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };
                var action = "EDIT";
                var activityLog = new Model.ActivityLog()
                {
                    UserId = user.Id,
                    ModuleName = _moduleName,
                    Action = action
                };

                var data = await _efDbContext.Set<Model.Route>()
                    .Include(r => r.Hosts)
                    .Include(r => r.IpRules)
                    .Include(r => r.Clients)
                    .FirstOrDefaultAsync(l => l.Id == Convert.ToInt64(model.Id));

                if (data != null)
                {
                    var oldValue = JsonConvert.SerializeObject(data, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

                    _efDbContext.Set<Model.RouteHost>().RemoveRange(data.Hosts);
                    _efDbContext.Set<Model.RouteIpRule>().RemoveRange(data.IpRules);
                    _efDbContext.Set<Model.RouteClient>().RemoveRange(data.Clients);

                    data.Hosts.Clear();
                    data.IpRules.Clear();
                    data.Clients.Clear();

                    var incomingHosts = model.Hosts;
                    var incomingIpRules = model.IpRules;
                    var incomingClients = model.Clients;

                    model.Hosts = new();
                    model.IpRules = new();
                    model.Clients = new();

                    _mapper.Map(model, data);

                    model.Hosts = incomingHosts;
                    model.IpRules = incomingIpRules;
                    model.Clients = incomingClients;

                    data.UpdatedBy = (int)user.Id;
                    data.UpdatedDate = DateTime.Now;

                    await _repository.UpdateAsync(data);

                    if (incomingHosts != null && incomingHosts.Any())
                    {
                        foreach (var h in incomingHosts)
                        {
                            data.Hosts.Add(new Model.RouteHost
                            {
                                RouteId = data.Id,
                                Host = h.Host,
                                Port = h.Port,
                                Description = h.Description
                            });
                        }
                    }

                    if (incomingIpRules != null && incomingIpRules.Any())
                    {
                        foreach (var i in incomingIpRules)
                        {
                            data.IpRules.Add(new Model.RouteIpRule
                            {
                                RouteId = data.Id,
                                IpAddressOrRange = i.IpAddressOrRange,
                                RuleType = i.RuleType,
                                Description = i.Description
                            });
                        }
                    }

                    if (incomingClients != null && incomingClients.Any())
                    {
                        foreach (var c in incomingClients)
                        {
                            data.Clients.Add(new Model.RouteClient
                            {
                                RouteId = data.Id,
                                ClientId = c.ClientId
                            });
                        }
                    }

                    await _efDbContext.SaveChangesAsync();

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = action,
                        ChangeBy = user.Id,
                        ActionDate = DateTime.Now,
                        TableName = "Master_Routes",
                        OriginalData = oldValue,
                        NewData = JsonConvert.SerializeObject(data, new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        })
                    };

                    activityLog.Details = string.Format("[{0}] definition updated.", data.Name);
                    result.Status = "SUCCESS";
                    result.Message = string.Format("{0} configuration updated.", _moduleName);

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(auditLog);
                }
                else
                {
                    result.Status = "FAILED";
                    result.Message = string.Format("{0} target config does not exist.", _moduleName);
                }

                transactionScope.Complete();
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
            finally
            {
                transactionScope.Dispose();
            }

            return result;
        }

        public async Task<Response.Result> Delete(Request.Route model)
        {
            Response.Result result = new();
            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "DELETE";
                var user = _efDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };

                var activityLog = new Model.ActivityLog()
                {
                    UserId = user.Id,
                    ModuleName = _moduleName,
                    Action = action
                };

                var data = await _efDbContext.Set<Model.Route>().FirstOrDefaultAsync(l => l.Id == Convert.ToInt64(model.Id));

                if (data != null)
                {
                    data.IsActive = false;
                    data.UpdatedBy = user.Id;
                    data.UpdatedDate = DateTime.Now;

                    await _repository.UpdateAsync(data);

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = action,
                        ChangeBy = user.Id,
                        ActionDate = DateTime.Now,
                        TableName = "Master_Routes",
                        OriginalData = "[IsActive : true]",
                        NewData = "[IsActive : false]"
                    };

                    activityLog.Details = string.Format("[{0}] gateway entry disabled.", data.Name);
                    result.Status = "SUCCESS";
                    result.Message = string.Format("{0} disabled successfully.", _moduleName);

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(auditLog);
                }
                else
                {
                    result.Status = "FAILED";
                    result.Message = string.Format("{0} reference location does not exist.", _moduleName);
                }

                transactionScope.Complete();
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
            finally
            {
                transactionScope.Dispose();
            }

            return result;
        }

        public async Task<Response.Result> Restore(Request.Route model)
        {
            Response.Result result = new();
            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "RESTORE";
                var user = _efDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };

                var activityLog = new Model.ActivityLog()
                {
                    UserId = user.Id,
                    ModuleName = _moduleName,
                    Action = action
                };

                var data = await _efDbContext.Set<Model.Route>().FirstOrDefaultAsync(l => l.Id == Convert.ToInt64(model.Id));

                if (data != null)
                {
                    data.IsActive = true;
                    data.UpdatedBy = user.Id;
                    data.UpdatedDate = DateTime.Now;

                    await _repository.UpdateAsync(data);

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = action,
                        ChangeBy = user.Id,
                        ActionDate = DateTime.Now,
                        TableName = "Master_Routes",
                        OriginalData = "[IsActive : false]",
                        NewData = "[IsActive : true]"
                    };

                    activityLog.Details = string.Format("[{0}] proxy path restored.", data.Name);
                    result.Status = "SUCCESS";
                    result.Message = string.Format("{0} route reactivated.", _moduleName);

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(auditLog);
                }
                else
                {
                    result.Status = "FAILED";
                    result.Message = string.Format("{0} targeted routing bounds missing.", _moduleName);
                }

                transactionScope.Complete();
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
            finally
            {
                transactionScope.Dispose();
            }

            return result;
        }

        public async Task<List<Response.RouteCategory>> GetCategory()
        {
            try
            {
                return await _efDbContext.Set<Model.RouteCategory>()
                    .Where(e => e.Deleted != true)
                    .Select(q => new Response.RouteCategory
                    {
                        Id = q.Id.ToString(),
                        Code = q.Code,
                        Description = q.Description!
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }
    }
}