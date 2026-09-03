using AutoMapper;
using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Gateway.Data.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Transactions;
using Model = Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services
{
    public class ApiEndpointService : IApiEndpointService
    {
        private readonly EFDbContext _efDbContext;
        private readonly IRepository<Model.ApiEndpoint> _repository;
        private readonly ILogService _logService;
        private readonly IMapper _mapper;
        private readonly DatabaseProxyConfigProvider _proxyConfigProvider;
        private readonly string _moduleName = "ApiEndpoint";

        public ApiEndpointService(
            EFDbContext efDbContext,
            IRepository<Model.ApiEndpoint> repository,
            ILogService logService,
            IMapper mapper,
            DatabaseProxyConfigProvider proxyConfigProvider)
        {
            _efDbContext = efDbContext;
            _repository = repository;
            _logService = logService;
            _mapper = mapper;
            _proxyConfigProvider = proxyConfigProvider;
        }

        public async Task<List<Response.ApiEndpoint>> GetActiveEndpointsAsync()
        {
            try
            {
                var endpoints = await _efDbContext.Set<Model.ApiEndpoint>()
                    .Include(e => e.Category)
                    .Include(e => e.TargetHosts)
                    .Include(e => e.IpRules)
                    .Where(e => e.IsActive)
                    .AsNoTracking()
                    .ToListAsync();

                return _mapper.Map<List<Response.ApiEndpoint>>(endpoints);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }

        public async Task<Response.VApiEndpoint> FilterAsync(Request.FParam model)
        {
            try
            {
                var propertySelector = EFramework.BuildPropertySelector<Model.ApiEndpoint>(model.SortColumn);
                Response.VApiEndpoint vData = new();

                var query = _efDbContext.Set<Model.ApiEndpoint>()
                    .Include(e => e.Category)
                    .Include(e => e.TargetHosts)
                    .Include(e => e.IpRules)
                    .AsQueryable();

                if (model.Filters != null && model.Filters.Count != 0)
                {
                    string filterValue = Convert.ToString(model.Filters[0].Value) ?? string.Empty;
                    query = query.Where(u => u.Name.Contains(filterValue)
                                          || u.UpstreamPathTemplate.Contains(filterValue)
                                          || (u.Category != null && u.Category.Name.Contains(filterValue)));
                }

                query = model.Descending
                    ? query.OrderByDescending(propertySelector)
                    : query.OrderBy(propertySelector);

                vData.CurrentPage = model.PageNum;
                vData.TotalRecord = await query.CountAsync();
                vData.TotalPage = (int)Math.Ceiling((double)vData.TotalRecord / model.PageSize);

                int recordsToSkip = (model.PageNum - 1) * model.PageSize;
                var pagedQuery = await query.Skip(recordsToSkip).Take(model.PageSize).ToListAsync();

                vData.Data = _mapper.Map<List<Response.FApiEndpoint>>(pagedQuery);

                return vData;
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }

        public async Task<Response.Result> CreateAsync(Request.ApiEndpoint model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var user = _efDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };
                var exists = await _efDbContext.Set<Model.ApiEndpoint>().AnyAsync(d => d.Code == model.Code);

                if (!exists)
                {
                    var data = _mapper.Map<Model.ApiEndpoint>(model);
                    data.RequireApiKey = model.RequireApiKey;
                    data.TargetHosts = new List<Model.TargetHost>();
                    data.IpRules = new List<Model.EndpointIpRule>();

                    data.CreatedBy = (int)user.Id;
                    data.CreatedDate = DateTime.Now;
                    data.IsActive = true;

                    await _repository.AddAsync(data);

                    if (model.TargetHosts != null && model.TargetHosts.Count > 0)
                    {
                        foreach (var h in model.TargetHosts)
                        {
                            data.TargetHosts.Add(new Model.TargetHost
                            {
                                Host = h.Host,
                                Port = h.Port,
                                Weight = h.Weight,
                                Description = h.Description
                            });
                        }
                    }

                    if (model.IpRules != null && model.IpRules.Count > 0)
                    {
                        foreach (var i in model.IpRules)
                        {
                            data.IpRules.Add(new Model.EndpointIpRule
                            {
                                IpAddressOrRange = i.IpAddressOrRange,
                                RuleType = i.RuleType,
                                Description = i.Description
                            });
                        }
                    }

                    await _efDbContext.SaveChangesAsync();

                    var auditLog = new Model.AuditLog
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal ?? "SYSTEM",
                        OperationType = "ADD",
                        ChangeBy = user.Id,
                        ActionDate = DateTime.Now,
                        TableName = "Master_ApiEndpoint",
                        OriginalData = "",
                        NewData = JsonConvert.SerializeObject(data, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })
                    };

                    _logService.LogActivity(new Model.ActivityLog { UserId = user.Id, ModuleName = _moduleName, Action = "ADD", Details = $"[{data.Name}] proxy path registered." });
                    _logService.LogAudit(auditLog);

                    transactionScope.Complete();

                    _proxyConfigProvider.UpdateConfig();

                    result = new Response.Result { Status = "SUCCESS", Message = $"{_moduleName} registered successfully." };
                }
                else
                {
                    result = new Response.Result { Status = "FAILED", Message = $"{_moduleName} endpoint code already exists." };
                }
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }

            return result;
        }

        public async Task<Response.Result> UpdateAsync(Request.ApiEndpoint model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var user = await _efDbContext.User!.FirstOrDefaultAsync(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };

                var data = await _efDbContext.Set<Model.ApiEndpoint>()
                    .Include(e => e.TargetHosts)
                    .Include(e => e.IpRules)
                    .FirstOrDefaultAsync(l => l.Id == Convert.ToInt64(model.Id));

                if (data != null)
                {
                    var oldValue = JsonConvert.SerializeObject(data, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

                    _efDbContext.Set<Model.TargetHost>().RemoveRange(data.TargetHosts);
                    _efDbContext.Set<Model.EndpointIpRule>().RemoveRange(data.IpRules);
                    data.TargetHosts.Clear();
                    data.IpRules.Clear();

                    var incomingHosts = model.TargetHosts;
                    var incomingIpRules = model.IpRules;
                    model.TargetHosts = new();
                    model.IpRules = new();

                    _mapper.Map(model, data);
                    data.RequireApiKey = model.RequireApiKey;

                    data.UpdatedBy = (int)user.Id;
                    data.UpdatedDate = DateTime.Now;

                    await _repository.UpdateAsync(data);

                    if (incomingHosts != null && incomingHosts.Count > 0)
                    {
                        foreach (var h in incomingHosts)
                        {
                            data.TargetHosts.Add(new Model.TargetHost
                            {
                                EndpointId = data.Id,
                                Host = h.Host,
                                Port = h.Port,
                                Weight = h.Weight,
                                Description = h.Description
                            });
                        }
                    }

                    if (incomingIpRules != null && incomingIpRules.Count > 0)
                    {
                        foreach (var i in incomingIpRules)
                        {
                            data.IpRules.Add(new Model.EndpointIpRule
                            {
                                EndpointId = data.Id,
                                IpAddressOrRange = i.IpAddressOrRange,
                                RuleType = i.RuleType,
                                Description = i.Description
                            });
                        }
                    }

                    await _efDbContext.SaveChangesAsync();

                    var auditLog = new Model.AuditLog
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal ?? "SYSTEM",
                        OperationType = "EDIT",
                        ChangeBy = user.Id,
                        ActionDate = DateTime.Now,
                        TableName = "Master_ApiEndpoint",
                        OriginalData = oldValue,
                        NewData = JsonConvert.SerializeObject(data, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })
                    };

                    _logService.LogActivity(new Model.ActivityLog { UserId = user.Id, ModuleName = _moduleName, Action = "EDIT", Details = $"[{data.Name}] definition updated." });
                    _logService.LogAudit(auditLog);

                    transactionScope.Complete();

                    _proxyConfigProvider.UpdateConfig();

                    result.Status = "SUCCESS";
                    result.Message = $"{_moduleName} configuration updated.";
                }
                else
                {
                    result.Status = "FAILED";
                    result.Message = $"{_moduleName} target endpoint does not exist.";
                }
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }

            return result;
        }

        public async Task<Response.Result> DeleteAsync(Request.ApiEndpoint model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var user = _efDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };
                var data = await _efDbContext.Set<Model.ApiEndpoint>().FirstOrDefaultAsync(l => l.Id == Convert.ToInt64(model.Id));

                if (data != null)
                {
                    data.IsActive = false;
                    data.UpdatedBy = (int)user.Id;
                    data.UpdatedDate = DateTime.Now;

                    await _repository.UpdateAsync(data);
                    await _efDbContext.SaveChangesAsync();

                    _logService.LogActivity(new Model.ActivityLog { UserId = user.Id, ModuleName = _moduleName, Action = "DELETE", Details = $"[{data.Name}] gateway entry disabled." });

                    transactionScope.Complete();

                    _proxyConfigProvider.UpdateConfig();

                    result.Status = "SUCCESS";
                    result.Message = $"{_moduleName} disabled successfully.";
                }
                else
                {
                    result.Status = "FAILED";
                    result.Message = $"{_moduleName} reference endpoint does not exist.";
                }
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }

            return result;
        }

        public async Task<Response.Result> RestoreAsync(Request.ApiEndpoint model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var user = _efDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };
                var data = await _efDbContext.Set<Model.ApiEndpoint>().FirstOrDefaultAsync(l => l.Id == Convert.ToInt64(model.Id));

                if (data != null)
                {
                    data.IsActive = true;
                    data.UpdatedBy = (int)user.Id;
                    data.UpdatedDate = DateTime.Now;

                    await _repository.UpdateAsync(data);
                    await _efDbContext.SaveChangesAsync();

                    _logService.LogActivity(new Model.ActivityLog { UserId = user.Id, ModuleName = _moduleName, Action = "RESTORE", Details = $"[{data.Name}] proxy path restored." });

                    transactionScope.Complete();

                    _proxyConfigProvider.UpdateConfig();

                    result.Status = "SUCCESS";
                    result.Message = $"{_moduleName} endpoint reactivated.";
                }
                else
                {
                    result.Status = "FAILED";
                    result.Message = $"{_moduleName} targeted endpoint missing.";
                }
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }

            return result;
        }
    }
}