using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Models = Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services
{
    public class OutboundAuthProfileService(
        EFDbContext dbContext,
        IRepository<Models.OutboundAuthProfile> repository,
        ILogService logService) : IOutboundAuthProfileService
    {
        private readonly EFDbContext _dbContext = dbContext;
        private readonly IRepository<Models.OutboundAuthProfile> _repository = repository;
        private readonly ILogService _logService = logService;
        private readonly string _moduleName = "OutboundAuthProfile";

        public async Task<List<Models.OutboundAuthProfile>> GetAllActiveAsync()
        {
            return await _dbContext.Set<Models.OutboundAuthProfile>()
                .Where(p => p.IsActive)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Models.OutboundAuthHeader>> GetHeadersByProfileIdAsync(int profileId)
        {
            return await _dbContext.Set<Models.OutboundAuthHeader>()
                .Where(h => h.ProfileId == profileId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Response.OutboundAuthProfile>> Get(string id)
        {
            try
            {
                var query = _dbContext.Set<Models.OutboundAuthProfile>().AsQueryable();

                if (!string.IsNullOrWhiteSpace(id) && int.TryParse(id, out int profileId))
                    query = query.Where(b => b.Id == profileId);

                var profiles = await query.ToListAsync();
                var profileIds = profiles.Select(p => p.Id).ToList();

                var headers = await _dbContext.Set<Models.OutboundAuthHeader>()
                    .Where(h => profileIds.Contains(h.ProfileId))
                    .ToListAsync();

                var result = profiles.Select(p => new Response.OutboundAuthProfile
                {
                    Id = p.Id.ToString(),
                    Code = p.Code,
                    Name = p.Name,
                    Description = p.Description ?? string.Empty,
                    IsActive = p.IsActive,
                    Headers = headers.Where(h => h.ProfileId == p.Id).Select(h => new Response.OutboundAuthHeader
                    {
                        Id = h.Id.ToString(),
                        AuthType = h.AuthType,
                        HeaderName = h.HeaderName ?? string.Empty,
                        CredentialValue = h.CredentialValue,
                        SecondaryCredentialValue = h.SecondaryCredentialValue ?? string.Empty
                    }).ToList()
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }

        public async Task<Response.VOutboundAuthProfile> Filter(Request.FParam model)
        {
            try
            {
                var propertySelector = EFramework.BuildPropertySelector<Models.OutboundAuthProfile>(model.SortColumn);
                Response.VOutboundAuthProfile vData = new();

                var query = _dbContext.Set<Models.OutboundAuthProfile>().AsQueryable();

                if (model.Filters != null && model.Filters.Count != 0)
                    query = query.Where(ExpressionBuilder.GetExpression<Models.OutboundAuthProfile>(model.Filters));

                query = model.Descending
                    ? query.OrderByDescending(propertySelector)
                    : query.OrderBy(propertySelector);

                vData.CurrentPage = model.PageNum;
                vData.TotalRecord = await query.CountAsync();
                vData.TotalPage = (int)Math.Ceiling((double)vData.TotalRecord / model.PageSize);

                int recordsToSkip = (model.PageNum - 1) * model.PageSize;
                var pagedProfiles = await query.Skip(recordsToSkip).Take(model.PageSize).ToListAsync();

                var profileIds = pagedProfiles.Select(p => p.Id).ToList();
                var headers = await _dbContext.Set<Models.OutboundAuthHeader>()
                    .Where(h => profileIds.Contains(h.ProfileId))
                    .ToListAsync();

                vData.Data = pagedProfiles.Select(u => new Response.FOutboundAuthProfile
                {
                    Id = u.Id.ToString(),
                    Code = u.Code,
                    Name = u.Name,
                    Description = u.Description ?? string.Empty,
                    IsActive = u.IsActive,
                    Headers = headers.Where(h => h.ProfileId == u.Id).Select(h => new Response.OutboundAuthHeader
                    {
                        Id = h.Id.ToString(),
                        AuthType = h.AuthType,
                        HeaderName = h.HeaderName ?? string.Empty,
                        CredentialValue = h.CredentialValue,
                        SecondaryCredentialValue = h.SecondaryCredentialValue ?? string.Empty
                    }).ToList()
                }).ToList();

                return vData;
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                return new();
            }
        }

        public async Task<Response.Result> Create(Request.OutboundAuthProfile model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "ADD";
                var user = await _dbContext.User.FirstOrDefaultAsync(u => u.Username == model.OpUser);
                int userId = user?.Id ?? 0;

                string cleanCode = model.Code.Trim().ToUpper();
                var exists = await _dbContext.Set<Models.OutboundAuthProfile>().AnyAsync(d => d.Code.ToUpper() == cleanCode);

                if (!exists)
                {
                    var profile = new Models.OutboundAuthProfile
                    {
                        Code = cleanCode,
                        Name = model.Name.Trim(),
                        Description = model.Description ?? string.Empty,
                        IsActive = true,
                        CreatedBy = userId,
                        CreatedDate = DateTime.Now
                    };

                    await _repository.AddAsync(profile);
                    await _dbContext.SaveChangesAsync();

                    if (model.Headers != null && model.Headers.Count != 0)
                    {
                        var headersToSave = model.Headers.Select(h => new Models.OutboundAuthHeader
                        {
                            ProfileId = profile.Id,
                            AuthType = string.IsNullOrWhiteSpace(h.AuthType) ? "APIKey" : h.AuthType,
                            HeaderName = string.IsNullOrWhiteSpace(h.HeaderName) ? "X-Api-Key" : h.HeaderName,
                            CredentialValue = h.CredentialValue ?? string.Empty,
                            SecondaryCredentialValue = h.SecondaryCredentialValue ?? string.Empty
                        }).ToList();

                        _dbContext.Set<Models.OutboundAuthHeader>().AddRange(headersToSave);
                        await _dbContext.SaveChangesAsync();
                    }

                    var auditLog = new Models.AuditLog
                    {
                        RecordId = profile.Id.ToString(),
                        Terminal = model.Terminal ?? "SYSTEM",
                        OperationType = action,
                        ChangeBy = userId,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = "",
                        NewData = JsonConvert.SerializeObject(profile, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })
                    };

                    _logService.LogActivity(new Models.ActivityLog { UserId = userId, ModuleName = _moduleName, Action = action, Details = $"[Code: {profile.Code}] created." });
                    _logService.LogAudit(auditLog);

                    result = new Response.Result { Status = "SUCCESS", Message = $"{_moduleName} created successfully." };
                }
                else
                {
                    result = new Response.Result { Status = "FAILED", Message = $"{_moduleName} code '{cleanCode}' already exists." };
                }

                transactionScope.Complete();
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                result = new Response.Result { Status = "ERROR", Message = "Error encountered during creation." };
            }

            return result;
        }

        public async Task<Response.Result> Update(Request.OutboundAuthProfile model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "EDIT";
                var user = await _dbContext.User.FirstOrDefaultAsync(u => u.Username == model.OpUser);
                int userId = user?.Id ?? 0;

                if (!int.TryParse(model.Id, out int profileId))
                    return new Response.Result { Status = "FAILED", Message = $"{_moduleName} invalid ID." };

                var profile = await _dbContext.Set<Models.OutboundAuthProfile>().FirstOrDefaultAsync(d => d.Id == profileId);

                if (profile != null)
                {
                    string cleanCode = model.Code.Trim().ToUpper();
                    var isCodeTaken = await _dbContext.Set<Models.OutboundAuthProfile>().AnyAsync(d => d.Code.ToUpper() == cleanCode && d.Id != profileId);

                    if (isCodeTaken)
                    {
                        return new Response.Result { Status = "FAILED", Message = $"{_moduleName} [Code: {cleanCode}] already exists." };
                    }

                    var oldValue = JsonConvert.SerializeObject(profile, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

                    profile.Code = cleanCode;
                    profile.Name = model.Name.Trim();
                    profile.Description = model.Description ?? string.Empty;
                    profile.IsActive = model.IsActive;
                    profile.UpdatedBy = userId;
                    profile.UpdatedDate = DateTime.Now;

                    await _repository.UpdateAsync(profile);

                    var oldHeaders = await _dbContext.Set<Models.OutboundAuthHeader>()
                        .Where(h => h.ProfileId == profileId)
                        .ToListAsync();

                    _dbContext.Set<Models.OutboundAuthHeader>().RemoveRange(oldHeaders);

                    if (model.Headers != null && model.Headers.Count != 0)
                    {
                        var newHeaders = model.Headers.Select(h => new Models.OutboundAuthHeader
                        {
                            ProfileId = profileId,
                            AuthType = string.IsNullOrWhiteSpace(h.AuthType) ? "APIKey" : h.AuthType,
                            HeaderName = string.IsNullOrWhiteSpace(h.HeaderName) ? "X-Api-Key" : h.HeaderName,
                            CredentialValue = h.CredentialValue ?? string.Empty,
                            SecondaryCredentialValue = h.SecondaryCredentialValue ?? string.Empty
                        }).ToList();

                        _dbContext.Set<Models.OutboundAuthHeader>().AddRange(newHeaders);
                    }

                    await _dbContext.SaveChangesAsync();

                    var auditLog = new Models.AuditLog
                    {
                        RecordId = profile.Id.ToString(),
                        Terminal = model.Terminal ?? "SYSTEM",
                        OperationType = action,
                        ChangeBy = userId,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = oldValue,
                        NewData = JsonConvert.SerializeObject(profile, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })
                    };

                    _logService.LogActivity(new Models.ActivityLog { UserId = userId, ModuleName = _moduleName, Action = action, Details = $"[Code: {cleanCode}] updated." });
                    _logService.LogAudit(auditLog);

                    result = new Response.Result { Status = "SUCCESS", Message = $"{_moduleName} updated successfully." };
                }
                else
                {
                    result = new Response.Result { Status = "FAILED", Message = $"{_moduleName} does not exist." };
                }

                transactionScope.Complete();
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                result = new Response.Result { Status = "ERROR", Message = "Error encountered during update." };
            }

            return result;
        }

        public async Task<Response.Result> Delete(Request.OutboundAuthProfile model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "DELETE";
                var user = await _dbContext.User.FirstOrDefaultAsync(u => u.Username == model.OpUser);
                int userId = user?.Id ?? 0;

                if (!int.TryParse(model.Id, out int profileId))
                    return new Response.Result { Status = "FAILED", Message = $"{_moduleName} invalid ID." };

                var profile = await _dbContext.Set<Models.OutboundAuthProfile>().FirstOrDefaultAsync(l => l.Id == profileId);

                if (profile != null)
                {
                    var oldValue = JsonConvert.SerializeObject(profile, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

                    profile.IsActive = false;
                    profile.UpdatedBy = userId;
                    profile.UpdatedDate = DateTime.Now;

                    await _repository.UpdateAsync(profile);
                    await _dbContext.SaveChangesAsync();

                    var auditLog = new Models.AuditLog
                    {
                        RecordId = profile.Id.ToString(),
                        Terminal = model.Terminal ?? "SYSTEM",
                        OperationType = action,
                        ChangeBy = userId,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = oldValue,
                        NewData = "[IsActive :false]"
                    };

                    _logService.LogActivity(new Models.ActivityLog { UserId = userId, ModuleName = _moduleName, Action = action, Details = $"[Code: {profile.Code}] deactivated." });
                    _logService.LogAudit(auditLog);

                    result = new Response.Result { Status = "SUCCESS", Message = $"{_moduleName} deactivated." };
                }
                else
                {
                    result = new Response.Result { Status = "FAILED", Message = $"{_moduleName} does not exist." };
                }

                transactionScope.Complete();
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                result = new Response.Result { Status = "ERROR", Message = "Error encountered during deletion." };
            }

            return result;
        }

        public async Task<Response.Result> Restore(Request.OutboundAuthProfile model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "RESTORE";
                var user = await _dbContext.User.FirstOrDefaultAsync(u => u.Username == model.OpUser);
                int userId = user?.Id ?? 0;

                if (!int.TryParse(model.Id, out int profileId))
                    return new Response.Result { Status = "FAILED", Message = $"{_moduleName} invalid ID." };

                var profile = await _dbContext.Set<Models.OutboundAuthProfile>().FirstOrDefaultAsync(l => l.Id == profileId);

                if (profile != null)
                {
                    profile.IsActive = true;
                    profile.UpdatedBy = userId;
                    profile.UpdatedDate = DateTime.Now;

                    await _repository.UpdateAsync(profile);
                    await _dbContext.SaveChangesAsync();

                    var auditLog = new Models.AuditLog
                    {
                        RecordId = profile.Id.ToString(),
                        Terminal = model.Terminal ?? "SYSTEM",
                        OperationType = action,
                        ChangeBy = userId,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = "[IsActive :false]",
                        NewData = "[IsActive :true]"
                    };

                    _logService.LogActivity(new Models.ActivityLog { UserId = userId, ModuleName = _moduleName, Action = action, Details = $"[Code: {profile.Code}] activated." });
                    _logService.LogAudit(auditLog);

                    result = new Response.Result { Status = "SUCCESS", Message = $"{_moduleName} restored." };
                }
                else
                {
                    result = new Response.Result { Status = "FAILED", Message = $"{_moduleName} does not exist." };
                }

                transactionScope.Complete();
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                result = new Response.Result { Status = "ERROR", Message = "Error encountered during restore." };
            }

            return result;
        }
    }
}