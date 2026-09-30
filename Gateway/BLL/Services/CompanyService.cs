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
using Model = Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services
{
    public class CompanyService(EFDbContext efDbContext, IRepository<Model.Company> repository, ILogService logService) : ICompanyService
    {
        private readonly EFDbContext _efDbContext = efDbContext;
        private readonly IRepository<Model.Company> _repository = repository;
        private readonly ILogService _logService = logService;
        private readonly string _moduleName = "Company";

        public async Task<List<Response.FCompany>> Get(string id)
        {
            try
            {
                var query = _efDbContext.Company!
                    .Where(e => e.Deleted != true);

                if (!string.IsNullOrWhiteSpace(id))
                    query = query.Where(b => b.Id == Convert.ToInt32(id));

                var result = await query.Select(q => new Response.FCompany
                {
                    Id = q.Id.ToString(),
                    Code = q.Code,
                    Description = q.Description ?? string.Empty,
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

        public async Task<Response.VCompany> Filter(Request.FParam model)
        {
            try
            {
                var propertySelector = EFramework.BuildPropertySelector<Model.Company>(model.SortColumn);
                Response.VCompany vCompany = new();

                var companyList = _efDbContext.Company!
                    .AsQueryable();

                if (model.Filters != null && model.Filters.Count != 0)
                    companyList = companyList.Where(ExpressionBuilder.GetExpression<Model.Company>(model.Filters));

                companyList = model.Descending
                    ? companyList.OrderBy(m => m.Deleted).ThenByDescending(propertySelector)
                    : companyList.OrderBy(m => m.Deleted).ThenBy(propertySelector);

                vCompany.CurrentPage = model.PageNum;
                vCompany.TotalRecord = await companyList.CountAsync();
                vCompany.TotalPage = (int)Math.Ceiling((double)vCompany.TotalRecord / model.PageSize);

                int recordsToSkip = (model.PageNum - 1) * model.PageSize;
                var pagedQuery = await companyList.Skip(recordsToSkip).Take(model.PageSize).ToListAsync();

                vCompany.Data = pagedQuery.Select(u => new Response.FCompany
                {
                    Id = u.Id.ToString(),
                    Code = u.Code,
                    Description = u.Description,
                    Deleted = u.Deleted
                }).ToList();

                return vCompany;
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                return new();
            }
        }

        public async Task<Response.Result> Create(Request.Company model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "ADD";
                var user = await _efDbContext.User!.FirstOrDefaultAsync(u => u.Username == model.OpUser);
                int userId = user?.Id ?? 0;

                string cleanCode = model.Code.Trim().ToUpper();
                var exists = await _efDbContext.Company!.AnyAsync(d => d.Code.ToUpper() == cleanCode && !d.Deleted);

                if (!exists)
                {
                    var data = new Model.Company
                    {
                        Code = cleanCode,
                        Description = model.Description,
                        CreatedBy = userId,
                        CreatedDate = DateTime.Now,
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

        public async Task<Response.Result> Update(Request.Company model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "EDIT";
                var user = await _efDbContext.User!.FirstOrDefaultAsync(u => u.Username == model.OpUser);
                int userId = user?.Id ?? 0;

                if (!int.TryParse(model.Id, out int companyId))
                    return new Response.Result { Status = "FAILED", Message = $"{_moduleName} invalid ID." };

                var data = await _efDbContext.Company!.FirstOrDefaultAsync(d => d.Id == companyId);

                if (data != null)
                {
                    string cleanCode = model.Code.Trim().ToUpper();
                    var isCodeTaken = await _efDbContext.Company!.AnyAsync(d => d.Code.ToUpper() == cleanCode && d.Id != companyId && !d.Deleted);

                    if (isCodeTaken)
                    {
                        return new Response.Result { Status = "FAILED", Message = $"{_moduleName} [Code: {cleanCode}] already exists." };
                    }

                    var oldValue = JsonConvert.SerializeObject(data, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

                    data.Code = cleanCode;
                    data.Description = model.Description;
                    data.UpdatedBy = userId;
                    data.UpdatedDate = DateTime.Now;

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

        public async Task<Response.Result> Delete(Request.Company model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "DELETE";
                var user = await _efDbContext.User!.FirstOrDefaultAsync(u => u.Username == model.OpUser);
                int userId = user?.Id ?? 0;

                var data = await _efDbContext.Company!.FirstOrDefaultAsync(l => l.Id.ToString() == model.Id);

                if (data != null)
                {
                    var oldValue = JsonConvert.SerializeObject(data, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

                    data.Deleted = true;
                    data.UpdatedBy = userId;
                    data.UpdatedDate = DateTime.Now;

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

        public async Task<Response.Result> Restore(Request.Company model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "RESTORE";
                var user = await _efDbContext.User!.FirstOrDefaultAsync(u => u.Username == model.OpUser);
                int userId = user?.Id ?? 0;

                var data = await _efDbContext.Company!.FirstOrDefaultAsync(l => l.Id.ToString() == model.Id);

                if (data != null)
                {
                    data.Deleted = false;
                    data.UpdatedBy = userId;
                    data.UpdatedDate = DateTime.Now;

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