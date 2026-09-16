using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Newtonsoft.Json; 
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

        public async Task<List<Response.Company>> Get(string id)
        {
            try
            {
                var query = _efDbContext.Company!
                    .Where(e => e.Deleted != true);

                if (id.Trim() != string.Empty)
                    query = query.Where(b => b.Id == Convert.ToInt32(id)); 

                query.AsQueryable(); 
                var result = (from q in query
                              select new Response.Company
                              {
                                  Id = Convert.ToString(q.Id),
                                  Code = q.Code,
                                  Description = q.Description!, 
                              }).ToList();

                return await Task.FromResult(result);
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

                //var query = _efDbContext.Company!.AsQueryable();
                var companyList = _efDbContext.Set<Model.Company>().AsQueryable();

                if (model.Filters != null && model.Filters.Count != 0)
                    companyList = companyList.Where(ExpressionBuilder.GetExpression<Model.Company>(model.Filters));

                if (model.Descending)
                {
                    companyList = companyList.OrderByDescending(propertySelector);
                }
                else
                {
                    companyList = companyList.OrderBy(propertySelector);
                }



                vCompany.CurrentPage = model.PageNum;
                vCompany.TotalRecord = companyList.Count();
                vCompany.TotalPage = (int)Math.Ceiling((double)vCompany.TotalRecord / model.PageSize);

                int recordsToSkip = (model.PageNum - 1) * model.PageSize;
                var pagedQuery = companyList.Skip(recordsToSkip).Take(model.PageSize);

                //vCompany.Data = _mapper.Map<List<Response.FCompany>>(pagedQuery.ToList());

                var result = (from u in pagedQuery
                              select new Response.FCompany
                              {
                                  Id = u.Id.ToString(),
                                  Code = u.Code,
                                  Description = u.Description,
                                  Deleted = u.Deleted
                              }
                               ).ToList();

                vCompany.Data = result;
                return await Task.FromResult(vCompany);
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

            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "ADD";
                var userId = _efDbContext.User!.FirstOrDefault(u => u.Username == model.OpUser)!.Id;

                var activityLog = new Model.ActivityLog()
                {
                    //UserId = operatorId!,
                    UserId = userId!,
                    ModuleName = _moduleName,
                    Action = action
                };

                var data = _efDbContext.Company!.FirstOrDefault(d => d.Code == model.Code);

                if (data == null)
                {

                    data = new()
                    { 
                        Code = model.Code,
                        Description = model.Description,
                        CreatedBy = userId,
                        CreatedDate = DateTime.Now
                    };

                    await _repository.AddAsync(data);

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = action,
                        //ChangeBy = operatorId!,
                        ChangeBy = userId!,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = "",
                        NewData = JsonConvert.SerializeObject(data, new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        })
                    };

                    activityLog.Details = string.Format("[code: {0}] created.", data.Code);

                    result = new Response.Result() { Status = "SUCCESS", Message = string.Format("{0} created.", _moduleName) };

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(auditLog!);

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
                result = new Response.Result() { Status = "ERROR", Message = "Error encountered" };
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                result = new Response.Result() { Status = "ERROR", Message = "Error encountered" };
            }
            finally
            {

                transactionScope.Dispose();
            }

            return await Task.FromResult(result);
        }

        public async Task<Response.Result> Update(Request.Company model)
        {
            Response.Result result = new();

            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "EDIT";
                var userId = _efDbContext.User!.FirstOrDefault(u => u.Username == model.OpUser)!.Id;

                int updateStatus = 0;

                var activityLog = new Model.ActivityLog()
                {
                    //UserId = operatorId!,
                    UserId = userId!,
                    ModuleName = _moduleName,
                    Action = action
                };

                var data = _efDbContext.Company!.Where(d => d.Id == Convert.ToInt32(model.Id!)).FirstOrDefault();

                if (data != null)
                {
                    if (data.Id == Convert.ToInt32(model.Id!) && data.Code == model.Code)
                    {
                        updateStatus = 1;
                    }
                    else
                    {
                        var x = _efDbContext.Company!.Any(d => d.Code == model.Code);

                        if (!x)
                            updateStatus = 1;
                        else
                        {
                            updateStatus = -1;
                            result.Status = "FAILED";
                            result.Message = string.Format("{0} [code: {1}] already exist.", _moduleName, model.Code);
                        }
                    }

                }
                else
                {
                    updateStatus = 0;
                    result.Status = "FAILED";
                    result.Message = string.Format("{0} not exist.", _moduleName);
                }


                if (updateStatus == 1)
                {
                    var oldValue = JsonConvert.SerializeObject(data!, new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });

                    data!.Code = model.Code;
                    data.Description = model.Description;
                    data.UpdatedBy = userId;
                    data.UpdatedDate = DateTime.Now;

                    await _repository.UpdateAsync(data);

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = action,
                        //ChangeBy = operatorId!,
                        ChangeBy = userId!,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = "",
                        NewData = JsonConvert.SerializeObject(data, new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        })
                    };

                    activityLog.Details = string.Format("[code: {0}] updated.", model.Code);
                    result.Status = "SUCCESS";
                    result.Message = string.Format("{0} updated.", _moduleName);

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(auditLog!);
                }

                transactionScope.Complete();

            }
            catch (TransactionAbortedException ex)
            {
                _logService.LogException(ex, _moduleName);
                result = new Response.Result() { Status = "ERROR", Message = "Error encountered" };
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                result = new Response.Result() { Status = "ERROR", Message = "Error encountered" };
            }
            finally
            {

                transactionScope.Dispose();
            }

            return await Task.FromResult(result);
        }

        public async Task<Response.Result> Delete(Request.Company model)
        {
            Response.Result result = new();
            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "DELETE";
                var userId = _efDbContext.User!.FirstOrDefault(u => u.Username == model.OpUser)!.Id;

                var activityLog = new Model.ActivityLog()
                {
                    //UserId = operatorId!,
                    UserId = userId,
                    ModuleName = _moduleName,
                    Action = action
                };

                var data = _efDbContext.Company!.FirstOrDefault(l => l.Id.ToString() == model.Id);

                if (data != null)
                {
                    var oldValue = JsonConvert.SerializeObject(data!, new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });

                    data.Deleted = true;
                    data.UpdatedBy = userId;
                    data.UpdatedDate = DateTime.Now;

                    await _repository.DeleteAsync(data);

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = action,
                        //ChangeBy = operatorId!,
                        ChangeBy = userId,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = "[deleted :false]",
                        NewData = "[deleted :true]"
                    };

                    activityLog.Details = string.Format("[code: {0}] deleted.", data.Code);
                    result.Status = "SUCCESS";
                    result.Message = string.Format("{0} deleted.", _moduleName);

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(auditLog!);

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
                result = new Response.Result() { Status = "ERROR", Message = "Error encountered" };
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                result = new Response.Result() { Status = "ERROR", Message = "Error encountered" };
            }
            finally
            {

                transactionScope.Dispose();
            }

            return await Task.FromResult(result);

        }
    }
}
