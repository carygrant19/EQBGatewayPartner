using AutoMapper;
using Gateway.BLL.Services;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Gateway.BLL.Helper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Transactions;
using Models = Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services
{
    public class ClientService(EFDbContext efDbContext, IConfiguration configuration, IMapper mapper, IRepository<Models.Client> ClientRepository, ILogService logService) : IClientService
    {

        private readonly EFDbContext _efDbContext = efDbContext;
        //private readonly IConfiguration _configuration = configuration;
        private readonly IRepository<Models.Client> _ClientRepository = ClientRepository;
        private readonly ILogService _logService = logService;
        private readonly IMapper _mapper = mapper;
        private readonly string _moduleName = "Client";
        private readonly string _encryptionKey = configuration["AppContext:EncryptionKey"]!;

        public async Task<Response.Client> Authenticate(string username, string password)
        {
            try
            { 
                var query = _efDbContext.Client!.FirstOrDefault(u => u.Username == username
                    && u.Password == StringManipulation.Encrypt(password, _encryptionKey)
                    && u.Deleted != true
                ); 

                return await Task.FromResult(_mapper.Map<Response.Client>(query));
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }

        }

        public async Task<Response.Client> ByApiKey(string key)
        {
            try
            {
                if (key == null || key == string.Empty)
                {
                    return new();
                }
                var result = _efDbContext.Set<Models.Client>()!.FirstOrDefault(b => b.ApiKey.ToString().ToUpper() == key.ToUpper() && b.Deleted != true); 
                return await Task.FromResult(_mapper.Map<Response.Client>(result));
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }
        public async Task<Response.Client> ByUsernameAndPassword(string username, string password)
        {
            try
            {
                if (username == null || username == string.Empty || password == null || password == string.Empty)
                {
                    return new();
                }
                var result = _efDbContext.Set<Models.Client>()!.FirstOrDefault(b => b.Username.ToString().ToUpper() == username.ToUpper() && b.Password == StringManipulation.Encrypt(password,_encryptionKey) && b.Deleted != true);
                return await Task.FromResult(_mapper.Map<Response.Client>(result));
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }
        public async Task<List<Response.Client>?> GetAll(bool includeDeleted)
        {
            try
            {
                var clientList = _efDbContext.Set<Models.Client>();

                if (!includeDeleted)
                    clientList.Where(e => e.Deleted != true);


                var companyList = _efDbContext.Set<Models.Company>();

                var result = (from u in clientList
                              join company in companyList on u.CompanyId equals company.Id
                              select new Response.Client
                              {
                                  Id = u.Id.ToString(),
                                  Username = u.Username,
                                  CompanyId = u.CompanyId.ToString(),
                                  CompanyCode = company.Code,
                                  CompanyDescription = company.Description,
                                  Description = u.Description,
                                  ApiKey = u.ApiKey,
                                  ApiSecret = u.ApiSecret,
                                  SSLRequired = u.SSLRequired,
                                  Deleted = u.Deleted
                              }
                                ).ToList();

                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                return new();
            }
        }

        public async Task<Response.Client> ById(string id)
        {
            try
            {
                var result = _efDbContext.Client!.FirstOrDefault(b => b.Id.ToString().ToUpper() == id && b.Deleted != true);

                return await Task.FromResult(_mapper.Map<Response.Client>(result));
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                return new();
            }

        }
        
        public async Task<Response.VClient> Filter(Request.FParam model)
        {
            try
            {
                var propertySelector = EFramework.BuildPropertySelector<Models.Client>(model.SortColumn);

                Response.VClient data = new();

                //var query = _efDbContext.Client!.AsQueryable();

                var clientList = _efDbContext.Set<Models.Client>().AsQueryable();


                if (model.Filters != null && model.Filters.Count != 0)
                    clientList = clientList.Where(ExpressionBuilder.GetExpression<Models.Client>(model.Filters));

                //var companyList = _efDbContext.Set<Company>();

                if (model.Descending)
                {
                    clientList = clientList.OrderByDescending(propertySelector);
                }
                else
                {
                    clientList = clientList.OrderBy(propertySelector);
                }

                data.CurrentPage = model.PageNum;
                data.TotalRecord = clientList.Count();
                data.TotalPage = (int)Math.Ceiling((double)data.TotalRecord / model.PageSize);

                int recordsToSkip = (model.PageNum - 1) * model.PageSize;
                var pagedQuery = clientList.Skip(recordsToSkip).Take(model.PageSize);

                //data.Data = _mapper.Map<List<Response.FClient>>(pagedQuery.ToList());


                var result = (from u in pagedQuery
                                  //join company in companyList on u.CompanyId equals company.Id
                              select new Response.FClient
                              {
                                  Id = u.Id.ToString().ToUpper(),
                                  Username = u.Username,
                                  CompanyId = u.CompanyId.ToString().ToUpper(),
                                  CompanyCode = u.Company.Code,
                                  CompanyDescription = u.Company.Description,
                                  Description = u.Description,
                                  Password = StringManipulation.Decrypt(u.Password, _encryptionKey),
                                  ApiKey = u.ApiKey,
                                  ApiSecret = u.ApiSecret,
                                  SSLRequired = u.SSLRequired,
                                  Deleted = u.Deleted
                              }
                               ).ToList();
                data.Data = result;
                return await Task.FromResult(data);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                return new();
            }
        }
        public async Task<Response.Result> Create(Request.Client model)
        {
            Response.Result result = new();
            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "ADD";
                var userId = _efDbContext.User!.FirstOrDefault(u => u.Username == model.OpUser)!.Id;

                var activityLog = new Models.ActivityLog()
                {
                    UserId = userId,
                    ModuleName = _moduleName,
                    Action = action
                };

                var data = _efDbContext.Client!.FirstOrDefault(u => u.Username == model.Username);

                if (data == null)
                {

                    data = new()
                    { 
                        Username = model.Username,
                        Password = StringManipulation.Encrypt(StringManipulation.Random(8), _encryptionKey), 
                        CompanyId = Convert.ToInt32(model.CompanyId),
                        Description = model.Description,
                        ApiKey = StringManipulation.Random(16),
                        ApiSecret = StringManipulation.Random(16),
                        SSLRequired = model.SSLRequired,
                        CreatedBy = userId,
                        CreatedDate = DateTime.Now
                    };

                    await _ClientRepository.AddAsync(data);

                    var AuditLog = new Models.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = action,
                        ChangeBy = userId,
                        ActionDate = (DateTime)data.CreatedDate,
                        TableName = _moduleName,
                        OriginalData = "",
                        NewData = JsonConvert.SerializeObject(data, new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        })
                    };

                    activityLog.Details = string.Format("[Username: {0}] created.", data.Username);

                    result = new Response.Result() { Status = "SUCCESS", Message = string.Format("{0} created.", _moduleName) };

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(AuditLog!);
                }
                else
                {
                    result = new Response.Result() { Status = "FAILED", Message = string.Format("Username: {0} already exist.", _moduleName) };
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
        public async Task<Response.Result> Update(Request.Client model)
        {
            Response.Result result = new();

            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "EDIT";
                var userId = _efDbContext.User!.FirstOrDefault(u => u.Username == model.OpUser)!.Id;

                int updateStatus = 0;

                var activityLog = new Models.ActivityLog()
                {
                    UserId = userId!,
                    ModuleName = _moduleName,
                    Action = action
                };

                var data = _efDbContext.Client!.FirstOrDefault(u => u.Username == model.Username);

                if (data != null)
                {
                    if (data.Id.ToString().ToUpper() == model.Id && data.Username == model.Username)
                    {
                        updateStatus = 1;
                    }
                    else
                    {
                        var x = _efDbContext.Client!.Any(d => d.Username == model.Username);

                        if (!x)
                            updateStatus = 1;
                        else
                        {
                            updateStatus = -1;
                            result.Status = "FAILED";
                            result.Message = string.Format("{0} [Username: {1}] already exist.", _moduleName, model.Username);
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

                    data!.Username = model.Username;
                    data!.CompanyId = Convert.ToInt32(model.CompanyId);
                    data.Description = model.Description;
                    data.SSLRequired = model.SSLRequired;
                    data.UpdatedBy = userId;
                    data.UpdatedDate = DateTime.Now;

                    await _ClientRepository.UpdateAsync(data);

                    var AuditLog = new Models.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = action,
                        ChangeBy = userId,
                        ActionDate = (DateTime)data.UpdatedDate,
                        TableName = _moduleName,
                        OriginalData = oldValue,
                        NewData = JsonConvert.SerializeObject(data, new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        })
                    }; 

                    activityLog.Details = string.Format("[Username: {0}] updated.", model.Username);
                    result.Status = "SUCCESS";
                    result.Message = string.Format("{0} updated.", _moduleName);

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(AuditLog!);
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

        public async Task<Response.Result> Delete(Request.Client model)
        {
            Response.Result result = new();
            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var _action = "DELETE";
                var userId = _efDbContext.User!.FirstOrDefault(u => u.Username == model.OpUser)!.Id;

                var activityLog = new Models.ActivityLog()
                {
                    UserId = userId,
                    ModuleName = _moduleName,
                    Action = _action
                };

                var data = _efDbContext.Client!.FirstOrDefault(u => u.Username == model.Username);

                if (data != null)
                {
                    var oldValue = JsonConvert.SerializeObject(data!, new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });

                    data.Deleted = true;
                    data.UpdatedBy = userId;
                    data.UpdatedDate = DateTime.Now;

                    await _ClientRepository.DeleteAsync(data);

                    var AuditLog = new Models.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = _action,
                        ChangeBy = userId,
                        ActionDate = (DateTime)data.UpdatedDate,
                        TableName = _moduleName,
                        OriginalData = "[deleted :false]",
                        NewData = "[deleted :true]"
                    };

                    activityLog.Details = string.Format("[Username: {0}] deleted.", data.Username);
                    result.Status = "SUCCESS";
                    result.Message = string.Format("{0} deleted.", _moduleName);

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(AuditLog!);

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

        public async Task<Response.Result> Restore(Request.Client model)
        {
            Response.Result result = new();
            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var _action = "RESTORE";
                var userId = _efDbContext.User!.FirstOrDefault(u => u.Username == model.OpUser)!.Id;

                var activityLog = new Models.ActivityLog()
                {
                    UserId = userId,
                    ModuleName = _moduleName,
                    Action = _action
                };

                var data = _efDbContext.Client!.FirstOrDefault(u => u.Username == model.Username);

                if (data != null)
                {
                    var oldValue = JsonConvert.SerializeObject(data!, new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });

                    data.Deleted = false;
                    data.UpdatedBy = userId;
                    data.UpdatedDate = DateTime.Now;

                    await _ClientRepository.DeleteAsync(data);

                    var AuditLog = new Models.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = _action,
                        ChangeBy = userId,
                        ActionDate = (DateTime)data.UpdatedDate,
                        TableName = _moduleName,
                        OriginalData = "[restored :false]",
                        NewData = "[restored :true]"
                    };

                    activityLog.Details = string.Format("[Username: {0}] deleted.", data.Username);
                    result.Status = "SUCCESS";
                    result.Message = string.Format("{0} deleted.", _moduleName);

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(AuditLog!);

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
        //reset api key & secret
        public async Task<Response.APISecurityResult> ResetAPIKeySecret(Request.Client model)
        {
            Response.APISecurityResult result = new();
            string newKey = StringManipulation.Random(16), newSecret = StringManipulation.Random(16);
            try
            {
                var action = "RESETAPIKEYSECRET";
                var userId = _efDbContext.User!.FirstOrDefault(u => u.Username == model.OpUser)!.Id;

                var activityLog = new Models.ActivityLog()
                {
                    UserId = userId,
                    ModuleName = _moduleName,
                    Action = action
                };

                var client = _efDbContext.Client!.FirstOrDefault(b => b.Id.ToString().ToUpper() == model.Id);
                if (client != null)
                {

                    client.ApiKey = newKey;
                    client.ApiSecret = newSecret;
                    await _ClientRepository.UpdateAsync(client);
                    result.Status = "SUCCESS";
                    result.Key = newKey;
                    result.Secret = newSecret;

                }
                else
                {
                    result.Status = "FAILED";
                    result.Key = "";
                    result.Secret = "";
                }
                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                result.Status = "ERROR";
                result.Key = "";
                result.Secret = "";
                return new();
            }

        }
        //reset password
        public async Task<Response.APISecurityResult> ResetPassword(Request.Client model)
        {
            Response.APISecurityResult result = new();
            string password = StringManipulation.Random(8);
            string newPassword = StringManipulation.Encrypt(password, _encryptionKey);
            try
            {
                var action = "RESETAPIKEYSECRET";
                var userId = _efDbContext.User!.FirstOrDefault(u => u.Username == model.OpUser)!.Id;

                var activityLog = new Models.ActivityLog()
                {
                    UserId = userId,
                    ModuleName = _moduleName,
                    Action = action
                };

                var client = _efDbContext.Client!.FirstOrDefault(b => b.Id.ToString().ToUpper() == model.Id);
                if (client != null)
                {

                    client.Password = newPassword;
                    await _ClientRepository.UpdateAsync(client);
                    result.Status = "SUCCESS";
                    result.Password = password;

                }
                else
                {
                    result.Status = "FAILED";
                    result.Password = "";
                }
                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                result.Status = "ERROR";
                result.Password = "";
                return new();
            }

        }
    }
}
