using AutoMapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Gateway.BLL.Services.IService;
using System.Transactions;
using Model = Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;
using Gateway.BLL.Helper;
using Gateway.BLL.Services.IServices;
using Microsoft.EntityFrameworkCore;

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
        private readonly string _encryptionKey = configuration["AppContext:EncryptionKey"]!;
        private readonly string _moduleName = "Route";

        public async Task<List<Response.Route>> Get()
        {
            try
            {
                // Pull active proxy routes along with their corresponding host maps and firewall rules
                var routes = await _efDbContext.Set<Model.Route>()
                    .Include(r => r.Hosts)
                    .Include(r => r.IpRules)
                    .Where(e => e.IsActive == true)
                    .ToListAsync();

                // Manually map to DTO structures to align with your encryption pattern bounds
                var result = routes.Select(q => new Response.Route
                {
                    Id = q.Id, // Primary keys for Routes are explicit varchar strings in your schema
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

                // Leverages AutoMapper to handle complex nested entity structures dynamically
                vData.Data = _mapper.Map<List<Response.FRoute>>(pagedQuery);

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

                // Check for duplicate endpoint routing registration structures
                var data = await _efDbContext.Set<Model.Route>().FirstOrDefaultAsync(d => d.Id.Trim().ToUpper() == model.Id.Trim().ToUpper());

                if (data == null)
                {
                    data = _mapper.Map<Model.Route>(model);
                    data.CreatedBy = model.OpUser ?? "SYSTEM";
                    data.CreatedDate = DateTime.Now;
                    data.IsActive = true;

                    // 1. Save core proxy routing parameters
                    await _repository.AddAsync(data);

                    // 2. Map and insert child load balancing server host instances
                    if (model.Hosts != null && model.Hosts.Any())
                    {
                        var hosts = model.Hosts.Select(h => new Model.RouteHost
                        {
                            RouteId = data.Id,
                            Host = h.Host,
                            Port = h.Port
                        }).ToList();
                        await _efDbContext.Set<Model.RouteHost>().AddRangeAsync(hosts);
                    }

                    // 3. Map and insert child access control ip restrictions
                    if (model.IpRules != null && model.IpRules.Any())
                    {
                        var rules = model.IpRules.Select(i => new Model.RouteIpRule
                        {
                            RouteId = data.Id,
                            IpAddressOrRange = i.IpAddressOrRange,
                            RuleType = i.RuleType
                        }).ToList();
                        await _efDbContext.Set<Model.RouteIpRule>().AddRangeAsync(rules);
                    }

                    await _efDbContext.SaveChangesAsync();

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id,
                        Terminal = model.Terminal!,
                        OperationType = action,
                        ChangeBy = user.Id!,
                        ActionDate = DateTime.Now,
                        TableName = "Routes",
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
                    result = new Response.Result() { Status = "FAILED", Message = string.Format("{0} routing key identifier already exists.", _moduleName) };
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
                var user = _efDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };
                var action = "EDIT";
                var activityLog = new Model.ActivityLog()
                {
                    UserId = user.Id,
                    ModuleName = _moduleName,
                    Action = action
                };
                // Explicit varchar keys do not require string decryption helpers
                var data = await _efDbContext.Set<Model.Route>()
                    .Include(r => r.Hosts)
                    .Include(r => r.IpRules)
                    .FirstOrDefaultAsync(l => l.Id == model.Id);

                if (data != null)
                {
                    var oldValue = JsonConvert.SerializeObject(data, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

                    // Map modified parent property primitives cleanly
                    _mapper.Map(model, data);
                    data.UpdatedBy = model.OpUser;
                    data.UpdatedDate = DateTime.Now;

                    await _repository.UpdateAsync(data);

                    // Re-align Child Records: Remove old load balancing instances and rewrite fresh properties
                    var oldHosts = _efDbContext.Set<Model.RouteHost>().Where(h => h.RouteId == data.Id);
                    _efDbContext.Set<Model.RouteHost>().RemoveRange(oldHosts);

                    if (model.Hosts != null && model.Hosts.Any())
                    {
                        var newHosts = model.Hosts.Select(h => new Model.RouteHost
                        {
                            RouteId = data.Id,
                            Host = h.Host,
                            Port = h.Port
                        }).ToList();
                        await _efDbContext.Set<Model.RouteHost>().AddRangeAsync(newHosts);
                    }

                    // Re-align Child Records: Purge old firewall tracking bounds and insert clean rules
                    var oldRules = _efDbContext.Set<Model.RouteIpRule>().Where(i => i.RouteId == data.Id);
                    _efDbContext.Set<Model.RouteIpRule>().RemoveRange(oldRules);

                    if (model.IpRules != null && model.IpRules.Any())
                    {
                        var newRules = model.IpRules.Select(i => new Model.RouteIpRule
                        {
                            RouteId = data.Id,
                            IpAddressOrRange = i.IpAddressOrRange,
                            RuleType = i.RuleType
                        }).ToList();
                        await _efDbContext.Set<Model.RouteIpRule>().AddRangeAsync(newRules);
                    }

                    await _efDbContext.SaveChangesAsync();

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id,
                        Terminal = model.Terminal!,
                        OperationType = action,
                        ChangeBy = user.Id!,
                        ActionDate = DateTime.Now,
                        TableName = "Routes",
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

                var data = await _efDbContext.Set<Model.Route>().FirstOrDefaultAsync(l => l.Id == model.Id);

                if (data != null)
                {
                    // Routes use IsActive flag states to cut traffic paths instantly instead of dropping table profiles
                    data.IsActive = false;
                    data.UpdatedBy = model.OpUser;
                    data.UpdatedDate = DateTime.Now;

                    await _repository.UpdateAsync(data);

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id,
                        Terminal = model.Terminal!,
                        OperationType = action,
                        ChangeBy = user.Id,
                        ActionDate = DateTime.Now,
                        TableName = "Routes",
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

                var data = await _efDbContext.Set<Model.Route>().FirstOrDefaultAsync(l => l.Id == model.Id);

                if (data != null)
                {
                    data.IsActive = true;
                    data.UpdatedBy = model.OpUser;
                    data.UpdatedDate = DateTime.Now;

                    await _repository.UpdateAsync(data);

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id,
                        Terminal = model.Terminal!,
                        OperationType = action,
                        ChangeBy = user.Id,
                        ActionDate = DateTime.Now,
                        TableName = "Routes",
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
    }
}