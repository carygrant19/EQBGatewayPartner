using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using Model = Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services
{
    public class AuthProviderService(EFDbContext efDbContext, IRepository<Model.AuthProvider> repository, ILogService logService) : IAuthProviderService
    {
        private readonly EFDbContext _efDbContext = efDbContext;
        private readonly IRepository<Model.AuthProvider> _repository = repository;
        private readonly ILogService _logService = logService;
        private readonly string _moduleName = "AuthProvider";
        public async Task<List<Response.FAuthProvider>> Get(string id)
        {
            try
            {
                var query = _efDbContext.AuthProvider!
                    .Where(e => e.Deleted != true);

                if (!string.IsNullOrWhiteSpace(id))
                    query = query.Where(b => b.Id == Convert.ToInt32(id));

                var result = await query.Select(q => new Response.FAuthProvider
                {
                    Id = q.Id.ToString(),
                    Code = q.Code,
                    Name = q.Name,
                    Issuer = q.Issuer,
                    Audience = q.Audience,
                    SecretKey = q.SecretKey,
                    TokenLifetimeMinutes = q.TokenLifetimeMinutes,
                    IsActive = q.IsActive,
                    Deleted = q.Deleted
                }).ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }

        public async Task<Response.VAuthProvider> Filter(Request.FParam model)
        {
            try
            {
                var propertySelector = EFramework.BuildPropertySelector<Model.AuthProvider>(model.SortColumn);
                Response.VAuthProvider vAuthProvider = new();

                var AuthProviderList = _efDbContext.AuthProvider!
                    .AsQueryable();

                if (model.Filters != null && model.Filters.Count != 0)
                    AuthProviderList = AuthProviderList.Where(ExpressionBuilder.GetExpression<Model.AuthProvider>(model.Filters));

                AuthProviderList = model.Descending
                    ? AuthProviderList.OrderBy(m => m.Deleted).ThenByDescending(propertySelector)
                    : AuthProviderList.OrderBy(m => m.Deleted).ThenBy(propertySelector);

                vAuthProvider.CurrentPage = model.PageNum;
                vAuthProvider.TotalRecord = await AuthProviderList.CountAsync();
                vAuthProvider.TotalPage = (int)Math.Ceiling((double)vAuthProvider.TotalRecord / model.PageSize);

                int recordsToSkip = (model.PageNum - 1) * model.PageSize;
                var pagedQuery = await AuthProviderList.Skip(recordsToSkip).Take(model.PageSize).ToListAsync();

                vAuthProvider.Data = pagedQuery.Select(u => new Response.FAuthProvider
                {
                    Id = u.Id.ToString(),
                    Code = u.Code,
                    Name = u.Name,
                    Issuer = u.Issuer,
                    Audience = u.Audience,
                    SecretKey = u.SecretKey,
                    TokenLifetimeMinutes = u.TokenLifetimeMinutes,
                    IsActive = u.IsActive,
                    Deleted = u.Deleted
                }).ToList();

                return vAuthProvider;
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                return new();
            }
        }

        public async Task<Response.Result> Create(Request.AuthProvider model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "ADD";
                var user = await _efDbContext.User!.FirstOrDefaultAsync(u => u.Username == model.OpUser);
                int userId = user?.Id ?? 0;

                string cleanCode = model.Code.Trim().ToUpper();
                var exists = await _efDbContext.AuthProvider!.AnyAsync(d => d.Code.ToUpper() == cleanCode && !d.Deleted);

                if (!exists)
                {
                    var data = new Model.AuthProvider
                    {
                        Code = cleanCode,
                        Name = model.Name,
                        Issuer = model.Issuer,
                        Audience = model.Audience,
                        SecretKey = model.SecretKey,
                        TokenLifetimeMinutes = model.TokenLifetimeMinutes,
                        IsActive = model.IsActive,
                        Deleted = false
                    }; 
                    await _repository.AddAsync(data);
                    await _efDbContext.SaveChangesAsync();

                    var auditLog = new Model.AuditLog
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal ?? "SYSTEM",
                        OperationType = action,
                        ChangeBy = userId,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = "",
                        NewData = JsonConvert.SerializeObject(data, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })
                    };

                    _logService.LogActivity(new Model.ActivityLog { UserId = userId, ModuleName = _moduleName, Action = action, Details = $"[Code: {data.Code}] created." });
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

        public async Task<Response.Result> Update(Request.AuthProvider model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "EDIT";
                var user = await _efDbContext.User!.FirstOrDefaultAsync(u => u.Username == model.OpUser);
                int userId = user?.Id ?? 0;

                if (!int.TryParse(model.Id, out int AuthProviderId))
                    return new Response.Result { Status = "FAILED", Message = $"{_moduleName} invalid ID." };

                var data = await _efDbContext.AuthProvider!.FirstOrDefaultAsync(d => d.Id == AuthProviderId);

                if (data != null)
                {
                    string cleanCode = model.Code.Trim().ToUpper();
                    var isCodeTaken = await _efDbContext.AuthProvider!.AnyAsync(d => d.Code.ToUpper() == cleanCode && d.Id != AuthProviderId && !d.Deleted);

                    if (isCodeTaken)
                    {
                        return new Response.Result { Status = "FAILED", Message = $"{_moduleName} [Code: {cleanCode}] already exists." };
                    }

                    var oldValue = JsonConvert.SerializeObject(data, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

                    data.Code = cleanCode;
                    data.Name = model.Name;
                    data.Issuer = model.Issuer;
                    data.Audience = model.Audience;
                    data.SecretKey = model.SecretKey;
                    data.TokenLifetimeMinutes = model.TokenLifetimeMinutes;
                    data.IsActive = model.IsActive;

 
                    await _repository.UpdateAsync(data);
                    await _efDbContext.SaveChangesAsync();

                    var auditLog = new Model.AuditLog
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal ?? "SYSTEM",
                        OperationType = action,
                        ChangeBy = userId,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = oldValue,
                        NewData = JsonConvert.SerializeObject(data, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })
                    };

                    _logService.LogActivity(new Model.ActivityLog { UserId = userId, ModuleName = _moduleName, Action = action, Details = $"[Code: {cleanCode}] updated." });
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

        public async Task<Response.Result> Delete(Request.AuthProvider model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "DELETE";
                var user = await _efDbContext.User!.FirstOrDefaultAsync(u => u.Username == model.OpUser);
                int userId = user?.Id ?? 0;

                var data = await _efDbContext.AuthProvider!.FirstOrDefaultAsync(l => l.Id.ToString() == model.Id);

                if (data != null)
                {
                    var oldValue = JsonConvert.SerializeObject(data, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

                    data.Deleted = true; 

                    await _repository.UpdateAsync(data);
                    await _efDbContext.SaveChangesAsync();

                    var auditLog = new Model.AuditLog
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal ?? "SYSTEM",
                        OperationType = action,
                        ChangeBy = userId,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = oldValue,
                        NewData = "[deleted :true]"
                    };

                    _logService.LogActivity(new Model.ActivityLog { UserId = userId, ModuleName = _moduleName, Action = action, Details = $"[Code: {data.Code}] deleted." });
                    _logService.LogAudit(auditLog);

                    result = new Response.Result { Status = "SUCCESS", Message = $"{_moduleName} deleted." };
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
                result = new Response.Result { Status = "ERROR", Message = "Error encountered during delete." };
            }

            return result;
        }

        public async Task<Response.Result> Restore(Request.AuthProvider model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "RESTORE";
                var user = await _efDbContext.User!.FirstOrDefaultAsync(u => u.Username == model.OpUser);
                int userId = user?.Id ?? 0;

                var data = await _efDbContext.AuthProvider!.FirstOrDefaultAsync(l => l.Id.ToString() == model.Id);

                if (data != null)
                {
                    data.Deleted = false; 

                    await _repository.UpdateAsync(data);
                    await _efDbContext.SaveChangesAsync();

                    var auditLog = new Model.AuditLog
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal ?? "SYSTEM",
                        OperationType = action,
                        ChangeBy = userId,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = "[deleted :true]",
                        NewData = "[deleted :false]"
                    };

                    _logService.LogActivity(new Model.ActivityLog { UserId = userId, ModuleName = _moduleName, Action = action, Details = $"[Code: {data.Code}] restored." });
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