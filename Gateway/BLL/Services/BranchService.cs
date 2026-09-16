using Gateway.BLL.Helper;
using Gateway.BLL.Services.IServices;
using Newtonsoft.Json; 
using System.Transactions;
using Model = Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;
namespace Gateway.BLL.Services
{
    public class BranchService(EFDbContext efDbContext, IRepository<Model.Branch> repository, ILogService logService) : IBranchService
    {
        private readonly EFDbContext _efDbContext = efDbContext;
        private readonly IRepository<Model.Branch> _repository = repository;
        private readonly ILogService _logService = logService; 
        private readonly string _moduleName = "Branch";

        public async Task<List<Response.Branch>> Get(string branch)
        {
            try
            {
                var query = _efDbContext.Branch!
                    .Where(e => e.Deleted != true);

                if (branch.Trim() != string.Empty)
                    query = query.Where(b => b.Code == branch); 

                query.AsQueryable(); 
                var result = (from q in query
                              select new Response.Branch
                              {
                                  Id = Convert.ToString(q.Id),
                                  Code = q.Code,
                                  Description = q.Description!,
                                  BankingHour = q.BankingHour!,
                                  Officer = q.Officer!,
                                  Address = q.Address!,
                                  ContactNo = q.ContactNo!,
                                  Email = q.Email!
                              }).ToList();

                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }

        public async Task<Response.VBranch> Filter(Request.FParam model)
        {
            try
            {
                var propertySelector = EFramework.BuildPropertySelector<Model.Branch>(model.SortColumn);

                Response.VBranch vData = new();

                var query = _efDbContext.Set<Model.Branch>()!.AsQueryable();

                if (model.Filters != null && model.Filters.Count != 0)
                    query = query.Where(ExpressionBuilder.GetExpression<Model.Branch>(model.Filters)!);

                query = model.Descending
                 ? query.OrderByDescending(propertySelector)
                 : query.OrderBy(propertySelector);

                vData.CurrentPage = model.PageNum;
                vData.TotalRecord = query.Count();
                vData.TotalPage = (int)Math.Ceiling((double)vData.TotalRecord / model.PageSize);

                int recordsToSkip = (model.PageNum - 1) * model.PageSize;
                var pagedQuery = query.Skip(recordsToSkip).Take(model.PageSize);

                var result = (from q in pagedQuery  
                              select new Response.FBranch
                              {
                                  Id = Convert.ToString(q.Id),
                                  Code = q.Code,
                                  Description = q.Description!,
                                  BankingHour = q.BankingHour!,
                                  Officer = q.Officer!,
                                  Address = q.Address!,
                                  ContactNo = q.ContactNo!,
                                  Email = q.Email!,
                                  
                                  Deleted = q.Deleted
                              }).ToList();
                //vData.Data = _mapper.Map<List<Response.FBranch>>(pagedQuery.ToList());
                vData.Data = result;

                return await Task.FromResult(vData);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }

        public async Task<Response.Result> Create(Request.Branch model)
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

                var data = _efDbContext.Branch!.FirstOrDefault(d => d.Code == model.Code);

                if (data == null)
                {

                    data = new()
                    {
                        Code = model.Code.Trim().ToUpper(),
                        Description = model.Description,
                        BankingHour = model.BankingHour,
                        Officer = model.Officer,
                        Address = model.Address,
                        ContactNo = model.ContactNo,
                        Email = model.Email,
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

        public async Task<Response.Result> Update(Request.Branch model)
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

                int id = Convert.ToInt32(model.Id!);
                var data = _efDbContext.Branch!.FirstOrDefault(l => l.Id == id);

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
                    data!.BankingHour = model.BankingHour;
                    data!.Officer = model.Officer;
                    data!.Address = model.Address;
                    data!.ContactNo = model.ContactNo;
                    data!.Email = model.Email;
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
        public async Task<Response.Result> Delete(Request.Branch model)
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

                int id = Convert.ToInt32(model.Id!);
                var data = _efDbContext.Branch!.FirstOrDefault(l => l.Id == id);

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

        public async Task<Response.Result> Restore(Request.Branch model)
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

                int id = Convert.ToInt32(model.Id!);
                var data = _efDbContext.Branch!.FirstOrDefault(l => l.Id == id);

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
