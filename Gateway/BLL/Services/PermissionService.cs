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

namespace Gateway.BLL.Services
{
    public class PermissionService(IConfiguration configuration, EFDbContext efDbContext, IMapper mapper, IRepository<Model.Permission> repository, ILogService logService) : IPermissionService
    {
        private readonly EFDbContext _efDbContext = efDbContext;
        private readonly IRepository<Model.Permission> _repository = repository;
        private readonly ILogService _logService = logService;
        private readonly IMapper _mapper = mapper;
        private readonly string _encryptionKey = configuration["AppContext:EncryptionKey"]!;
        private readonly string _moduleName = "Permission";

        public async Task<List<Response.Permission>> Get()
        {
            try
            {
                var permissions = _efDbContext.Set<Model.Permission>().Where(e => e.Deleted != true).AsQueryable();
                //.ProjectTo<Response.Permission>(_mapper.ConfigurationProvider)!.ToList();
                var result = (from q in permissions
                              select new Response.Permission
                              {
                                  Id = q.Id.ToString(),
                                  Code = q.Code,
                                  Description = q.Description!
                              }
                ).ToList();

                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }

        public async Task<Response.VPermission> Filter(Request.FParam model)
        {
            try
            {
                var propertySelector = EFramework.BuildPropertySelector<Model.Permission>(model.SortColumn);

                Response.VPermission vData = new();

                var query = _efDbContext.Set<Model.Permission>()!.AsQueryable();

                if (model.Filters != null && model.Filters.Count != 0)
                    query = query.Where(ExpressionBuilder.GetExpression<Model.Permission>(model.Filters)!);

                query = model.Descending
                 ? query.OrderByDescending(propertySelector)
                 : query.OrderBy(propertySelector);

                vData.CurrentPage = model.PageNum;
                vData.TotalRecord = query.Count();
                vData.TotalPage = (int)Math.Ceiling((double)vData.TotalRecord / model.PageSize);

                int recordsToSkip = (model.PageNum - 1) * model.PageSize;
                var pagedQuery = query.Skip(recordsToSkip).Take(model.PageSize);

                vData.Data = _mapper.Map<List<Response.FPermission>>(pagedQuery.ToList());

                return await Task.FromResult(vData);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }

        public async Task<Response.Result> Create(Request.Permission model)
        {
            Response.Result result = new();

            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "ADD";
                var user = _efDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };
                //var operatorId = Convert.ToInt32(model.OpUserId);

                var activityLog = new Model.ActivityLog()
                {
                    //UserId = operatorId!,
                    UserId = user.Id!,
                    ModuleName = _moduleName,
                    Action = action
                };

                var data = _efDbContext.Permission!.FirstOrDefault(d => d.Code == model.Code);

                if (data == null)
                {

                    data = new()
                    {
                        Code = model.Code.Trim().ToUpper(),
                        Description = model.Description,
                    };

                    await _repository.AddAsync(data);

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = action,
                        //ChangeBy = operatorId!,
                        ChangeBy = user.Id!,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = "",
                        NewData = JsonConvert.SerializeObject(data, new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        })
                    };

                    activityLog.Details = string.Format("[{0}] created.", data.Code);

                    result = new Response.Result() { Status = "SUCCESS", Message = string.Format("{0} created.", _moduleName) };

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(auditLog);

                }
                else
                {
                    result = new Response.Result() { Status = "FAILED", Message = string.Format("{0} already exist.", _moduleName) };
                }

                transactionScope.Complete();

            }
            catch (TransactionAbortedException ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
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

            return await Task.FromResult(result);
        }

        public async Task<Response.Result> Update(Request.Permission model)
        {
            Response.Result result = new();

            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var user = _efDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };
                //var operatorId = Convert.ToInt32(model.OpUserId);
                var action = "EDIT";

                int updateStatus = 0;

                var activityLog = new Model.ActivityLog()
                {
                    //UserId = operatorId!,
                    UserId = user.Id!,
                    ModuleName = _moduleName,
                    Action = action
                };


                int id = Convert.ToInt32(StringManipulation.Decrypt(model.Id!, _encryptionKey));
                var data = _efDbContext.Permission!.FirstOrDefault(l => l.Id == id);

                if (data != null)
                {
                    updateStatus = 1;
                }
                else
                {
                    updateStatus = 0;
                    result.Status = "FAILED";
                    result.Message = string.Format("{0} not exist.", _moduleName);
                }


                if (updateStatus == 1)
                {
                    var oldValue = JsonConvert.SerializeObject(data!);

                    //data!.Code = model.Code;
                    data!.Description = model.Description;

                    await _repository.UpdateAsync(data);

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = action,
                        //ChangeBy = operatorId!,
                        ChangeBy = user.Id!,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = "",
                        NewData = JsonConvert.SerializeObject(data, new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        })
                    };

                    activityLog.Details = string.Format("[{0}] updated.", data.Code);
                    result.Status = "SUCCESS";
                    result.Message = string.Format("{0} updated.", _moduleName);

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(auditLog);
                }

                transactionScope.Complete();

            }
            catch (TransactionAbortedException ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
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

            return await Task.FromResult(result);
        }
        public async Task<Response.Result> Delete(Request.Permission model)
        {
            Response.Result result = new();
            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "DELETE";
                var user = _efDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };
                //var operatorId = Convert.ToInt32(model.OpUserId);
                var activityLog = new Model.ActivityLog()
                {
                    //UserId = operatorId!,
                    UserId = user.Id,
                    ModuleName = _moduleName,
                    Action = action
                };

                int id = Convert.ToInt32(StringManipulation.Decrypt(model.Id!, _encryptionKey));
                var data = _efDbContext.Permission!.FirstOrDefault(l => l.Id == id);

                if (data != null)
                {
                    var oldValue = JsonConvert.SerializeObject(data!);

                    data.Deleted = true;

                    await _repository.DeleteAsync(data);

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = action,
                        //ChangeBy = operatorId!,
                        ChangeBy = user.Id,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = "[deleted :false]",
                        NewData = "[deleted :true]"
                    };

                    activityLog.Details = string.Format("[{0}] deleted.", data.Code);
                    result.Status = "SUCCESS";
                    result.Message = string.Format("{0} deleted.", _moduleName);

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(auditLog);

                }
                else
                {
                    result.Status = "FAILED";
                    result.Message = string.Format("{0} not exist.", _moduleName);
                }

                transactionScope.Complete();

            }
            catch (TransactionAbortedException ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
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

            return await Task.FromResult(result);

        }

        public async Task<Response.Result> Restore(Request.Permission model)
        {
            Response.Result result = new();
            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "RESTORE";
                var user = _efDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };
                //var operatorId = Convert.ToInt32(model.OpUserId);
                var activityLog = new Model.ActivityLog()
                {
                    UserId = user.Id,
                    ModuleName = _moduleName,
                    Action = action
                };

                int id = Convert.ToInt32(StringManipulation.Decrypt(model.Id!, _encryptionKey));
                var data = _efDbContext.Permission!.FirstOrDefault(l => l.Id == id);

                if (data != null)
                {
                    var oldValue = JsonConvert.SerializeObject(data!);

                    data.Deleted = false;

                    await _repository.DeleteAsync(data);

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = action,
                        //ChangeBy = operatorId!,
                        ChangeBy = user.Id,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = "[deleted :false]",
                        NewData = "[deleted :true]"
                    };

                    activityLog.Details = string.Format("[{0}] restored.", data.Code);
                    result.Status = "SUCCESS";
                    result.Message = string.Format("{0} restored.", _moduleName);

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(auditLog);

                }
                else
                {
                    result.Status = "FAILED";
                    result.Message = string.Format("{0} not exist.", _moduleName);
                }

                transactionScope.Complete();

            }
            catch (TransactionAbortedException ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
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

            return await Task.FromResult(result);

        }
    }
}
