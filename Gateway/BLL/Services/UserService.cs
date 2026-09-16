using Gateway.BLL.Helper;
using Response = Gateway.BLL.DTO.Response;
using Request = Gateway.BLL.DTO.Request;
using Model = Gateway.Data.Models;
using Microsoft.Extensions.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.Transactions;
using Newtonsoft.Json; 
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace Gateway.BLL.Services
{
    public class UserService(IConfiguration configuration
        , EFDbContext efDbContext
        , IRepository<Model.User> repository
        , IRepository<Model.ActiveUser> activeUserRepository
        , IRepository<Model.UserRole> userRoleRepository
        , IRepository<Model.PasswordHistory> passwordHistoryRepository
        , IMailerService mailerService
        , ILogService logService) : IUserService

    {

        private readonly EFDbContext _applicationDbContext = efDbContext;
        private readonly IRepository<Model.User> _repository = repository;
        private readonly IRepository<Model.UserRole> _userRoleRepository = userRoleRepository;
        private readonly IRepository<Model.ActiveUser> _activeUserRepository = activeUserRepository;
        private readonly IRepository<Model.PasswordHistory> _passwordHistoryRepository = passwordHistoryRepository;
        private readonly IMailerService _mailerService = mailerService;
        private readonly ILogService _logService = logService; 
        private readonly string _encryptionKey = configuration["AppContext:EncryptionKey"]!;
        private readonly string _moduleName = "User";
        private readonly Model.SystemParameters _systemParameters = configuration.GetSection("SystemParameters").Get<Model.SystemParameters>()!;
        private readonly Model.MailSettings mailSettings = configuration.GetSection("MailSettings").Get<Model.MailSettings>()!;

        public async Task<Response.VActivityLog> FilterActivityLog(Request.FParam model)
        {
            try
            {
                var propertySelector = EFramework.BuildPropertySelector<Model.ActivityLog>(model.SortColumn);
                Response.VActivityLog vData = new();
                var query = _applicationDbContext.Set<Model.ActivityLog>().AsQueryable();

                if (model.Filters != null && model.Filters.Count != 0)
                    query = query.Where(ExpressionBuilder.GetExpression<Model.ActivityLog>(model.Filters)!);

                if (model.Descending)
                {
                    query = query.OrderByDescending(propertySelector);
                }
                else
                {
                    query = query.OrderBy(propertySelector);
                }

                vData.CurrentPage = model.PageNum;
                vData.TotalRecord = query.Count();
                vData.TotalPage = (int)Math.Ceiling((double)vData.TotalRecord / model.PageSize);

                int recordsToSkip = (model.PageNum - 1) * model.PageSize;
                var pagedQuery = query.Skip(recordsToSkip).Take(model.PageSize);

                var result = (from u in pagedQuery
                              select new Response.FActivityLog
                              {
                                  Id = StringManipulation.Encrypt(Convert.ToString(u.Id), _encryptionKey),
                                  UserId = u.UserId,
                                  ModuleName = u.ModuleName,
                                  Action = u.Action,
                                  Details = u.Details,
                                  LogDate = u.LogDate
                              })
                              .OrderByDescending(u => u.LogDate)
                              .ToList();
                vData.Data = result;

                return await Task.FromResult(vData);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }
        public async Task<List<Response.User>> Get()
        {
            try
            {
                var query = _applicationDbContext.Set<Model.User>()
                    .Where(e => e.Deleted != true);
                var result = (from u in query
                              select new Response.User
                              {
                                  Id = StringManipulation.Encrypt(Convert.ToString(u.Id), _encryptionKey),
                                  Username = u.Username,
                                  FirstName = u.FirstName,
                                  MiddleName = u.MiddleName,
                                  LastName = u.LastName,
                              })
                              .OrderBy(u => u.Username)
                              .ToList();
                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }
        public async Task<Response.Result> Validate(Request.User model)
        {

            var action = "LOGIN";
            var result = new Response.Result();


            try
            {
                var pass = StringManipulation.Encrypt(model.Password!, _encryptionKey);
                var _user = _applicationDbContext.User!
                    .FirstOrDefault(u => u.Username!.ToUpper() == model.Username.ToUpper() && u.Password == StringManipulation.Encrypt(model.Password!, _encryptionKey));


                // Initialize activity log
                var activityLog = new Model.ActivityLog
                {
                    UserId = 0,
                    ModuleName = _moduleName,
                    Action = action
                };

                if (_user != null)
                {
                    var operatorId = _user!.Id;
                    activityLog.UserId = _user.Id;
                    if (_user.PasswordAttempt > _systemParameters.PasswordMaxTry)
                    {
                        result = new Response.Result { Status = "LOCKED", Message = "Account is locked." };
                        activityLog.Details = "Account is locked.";
                    }
                    else if (_user.Deleted == true)
                    {
                        result = new Response.Result { Status = "DELETED", Message = "Account is deleted." };
                        activityLog.Details = "Account is deleted.";
                    }
                    else if (DateTime.Now > _user.PasswordExpirationDate || _user.DefaultPassword == true)
                    {
                        result = new Response.Result { Status = "EXPRPASS", Message = "Using default or expired password." };
                        activityLog.Details = "Password expired or using default password.";
                    }
                    else
                    {
                        result = new Response.Result { Status = "SUCCESS", Message = "Authenticated." };
                        activityLog.Details = "Authenticated.";

                        await _activeUserRepository.AddAsync(new Model.ActiveUser
                        {
                            UserId = _user.Id,
                            Terminal = model.Terminal,
                            ActivityDate = DateTime.Now
                        });
                        // Reset password attempts
                        using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                        {

                            //_applicationDbContext.Database.ExecuteSql($"UPDATE [User] SET [PasswordAttempt] = 0 WHERE [Username] = '{model.Username}';");

                            _user!.PasswordAttempt = 0;

                            await _repository.UpdateAsync(_user);


                            _logService.LogActivity(activityLog);
                            transactionScope.Complete();
                        }
                    }
                }
                else
                {
                    using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                    {
                        //_applicationDbContext.Database.ExecuteSql($"UPDATE [master_user] SET [PasswordAttempt] = [PasswordAttempt] + 1 WHERE [Username] = '{model.Username}';");
                        var _user2 = _applicationDbContext.User!
                            .FirstOrDefault(u => u.Username!.ToUpper() == model.Username.ToUpper());
                        if (_user2 != null)
                        {
                            _user2!.PasswordAttempt += 1;

                            await _repository.UpdateAsync(_user2);

                            result = new Response.Result { Status = "INVLD", Message = "Invalid username or password." };
                            _logService.LogActivity(activityLog);
                            transactionScope.Complete();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
            return await Task.FromResult(result);
        }
        public async Task LogActiveUser(Request.User model)
        {
            var _user = _applicationDbContext.User!
                   .FirstOrDefault(u => u.Username!.ToUpper() == model.Username.ToUpper());
            await _activeUserRepository.AddAsync(new Model.ActiveUser
            {
                UserId = _user.Id,
                Terminal = model.Terminal,
                ActivityDate = DateTime.Now
            });
        }
        public async Task<Response.User> ByUsername(string username)
        {
            try
            {
                //var result = _applicationDbContext.User!.Include(b => b.Branch).FirstOrDefault(
                //    u => u.Username!.Trim().Equals(username!.Trim(), StringComparison.CurrentCultureIgnoreCase)
                //    && u.Deleted != true);

                var result = _applicationDbContext.User!.Include(b => b.Branch).FirstOrDefault(
                     u => u.Username!.Trim().ToUpper() == username!.Trim().ToUpper()
                     //&& u.Deleted != true
                     );

                var userInfo = new Response.User();
                if (result != null)
                {
                    userInfo.Id = Convert.ToString(result.Id);
                    userInfo.Username = result.Username;
                    userInfo.FirstName = result.FirstName;
                    userInfo.MiddleName = result.MiddleName;
                    userInfo.LastName = result.LastName;
                    userInfo.Email = result.Email;
                    userInfo.LDAPAuthentication = result.LDAPAuthentication;
                    userInfo.Branch = result.Branch != null ? new Response.Branch
                    {
                        Code = result.Branch.Code,
                        Description = result.Branch.Description!
                    } : null;
                    userInfo.PasswordAttempt = result.PasswordAttempt;
                    userInfo.PasswordExpirationDate = result.PasswordExpirationDate;
                    userInfo.PasswordLastChange = result.PasswordLastChange;
                    userInfo.DefaultPassword = result.DefaultPassword;
                    userInfo.Deleted = result.Deleted;
                    userInfo.ImageContent = result.ImageContent;
                    userInfo.ImageContentThumbnail = result.ImageContentThumbnail;
                    userInfo.ImageType = result.ImageType;
                }

                return await Task.FromResult(userInfo);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }
        public async Task<bool> CheckActiveSession(Request.User user)
        {
            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);
            try
            {
                double? sessionTimeoutMinutes = 0;
                if (_systemParameters != null)
                {
                    sessionTimeoutMinutes = _systemParameters.SessionTimeOutMinutes <= 0 ? 3 : _systemParameters.SessionTimeOutMinutes;
                }
                else
                {
                    sessionTimeoutMinutes = 3;
                }

                var id = _applicationDbContext.Set<Model.User>()
                     .Where(u => u.Username!.ToUpper() == user.Username.ToUpper() && u.Deleted == false)
                     .FirstOrDefault()!.Id;

                var userActivity = await _applicationDbContext.ActiveUser!
                    .Where(u => u.UserId == id)
                    .GroupBy(u => u.UserId)
                    .Select(g => new
                    {
                        Count = g.Count(),
                        LatestActivity = g.Max(u => u.ActivityDate),
                        Terminal = g.OrderByDescending(u => u.ActivityDate).Select(u => u.Terminal).FirstOrDefault()
                    })
                    .FirstOrDefaultAsync();

                if (userActivity != null && userActivity.Count > 0)
                {
                    if (userActivity.Terminal != user.Terminal)
                    {
                        if (DateTime.Now.Subtract((DateTime)userActivity.LatestActivity!).TotalMinutes >= sessionTimeoutMinutes)
                        {
                            return await Task.FromResult(false);
                        }
                        else
                        {
                            return await Task.FromResult(true);
                        }
                    }
                    else
                    {
                        return await Task.FromResult(false);
                    }
                }
                else
                {
                    return await Task.FromResult(false);
                }
            }
            catch (TransactionAbortedException ex)
            {
                _logService.LogException(ex, _moduleName);
                return await Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                return await Task.FromResult(false);
            }
            finally
            {
                transactionScope.Dispose();
            }
        }
        public async Task<int> GetRemainingDaysPasswordExpiry(string username)
        {
            int daysRemaining = -1;
            try
            {
                var user = _applicationDbContext.User!.FirstOrDefault(u => u.Username!.ToUpper() == username.ToUpper());

                if (user != null)
                {
                    DateTime currentDate = DateTime.Now.Date;
                    daysRemaining = (user.PasswordExpirationDate! - currentDate).Value.Days;
                }
            }
            catch (Exception ex)
            {

            }
            return await Task.FromResult(daysRemaining);
        }
        public async Task<Response.VUser> Filter(Request.FParam model)
        {
            try
            {
                var propertySelector = EFramework.BuildPropertySelector<Model.User>(model.SortColumn);
                Response.VUser vData = new();

                var query = _applicationDbContext.User!
                    .Include(u => u.UserRoles)!.ThenInclude(r => r.Role)
                    .AsQueryable();
  
                if (model.Filters != null && model.Filters.Count != 0)
                {
                    query = query.Where(u => u.FullName.Contains(Convert.ToString(model.Filters[0].Value)) || u.Username.Contains(Convert.ToString(model.Filters[0].Value)));
                }

                vData.CurrentPage = model.PageNum;
                vData.TotalRecord = await query.CountAsync(); 
                vData.TotalPage = (int)Math.Ceiling((double)vData.TotalRecord / model.PageSize);

                int recordsToSkip = (model.PageNum - 1) * model.PageSize;
                 
                if (model.Descending)
                {
                    query = query.OrderByDescending(propertySelector);
                }
                else
                {
                    query = query.OrderBy(propertySelector);
                }
                 
                var result = await query
                    .Skip(recordsToSkip)
                    .Take(model.PageSize)
                    .Select(u => new Response.FUser
                    {
                        Id = StringManipulation.Encrypt(u.Id.ToString(), _encryptionKey),
                        Username = u.Username,
                        FirstName = u.FirstName,
                        MiddleName = u.MiddleName,
                        LastName = u.LastName,
                        Email = u.Email,
                        Branch = u.Branch != null ? new Response.Branch
                        {
                            Id = u.BranchId.ToString(),
                            Code = u.Branch.Code,
                            Description = u.Branch.Description!
                        } : null,
                        LDAPAuthentication = u.LDAPAuthentication,
                        ImageContent = u.ImageContentThumbnail,
                        ImageType = u.ImageType,
                        IsLocked = Convert.ToBoolean(u.Status),
                        UserRoles = u.UserRoles!.Select(userRole => new Response.UserRole
                        {
                            Id = userRole.Id.ToString(),
                            UserId = userRole.Id.ToString(),
                            Username = u.Username!,
                            RoleId = userRole.RoleId.ToString(),
                            RoleCode = userRole.Role!.Code,
                            RoleDesc = userRole.Role.Description,
                        }).ToList(),
                        Deleted = u.Deleted
                    }).ToListAsync(); // Execute the query asynchronously

                vData.Data = result;
                return vData;
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }

        }
        public async Task<Response.Result> Create(Request.User model)
        {
            Response.Result result = new();

            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "ADD";

                //var @operator = _applicationDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new User() { Id = 0 };
                var operatorId = Convert.ToInt32(model.OpUserId);


                var data = _applicationDbContext.User!.FirstOrDefault(d => d.Username == model.Username);

                var activityLog = new Model.ActivityLog()
                {
                    UserId = operatorId!,
                    ModuleName = _moduleName,
                    Action = action
                };

                if (data == null)
                {
                    var password = StringManipulation.Random(6);
                    data = new()
                    {
                        Username = model.Username.Trim().ToUpper(),
                        Password = StringManipulation.Encrypt(password, _encryptionKey),
                        FirstName = model.FirstName,
                        MiddleName = model.MiddleName,
                        LastName = model.LastName,
                        Email = model.Email,
                        BranchId = Convert.ToInt32(model.Branch),
                        DefaultPassword = true,
                        LDAPAuthentication = Convert.ToBoolean(model.LDAPAuthentication),
                    };


                    await _repository.AddAsync(data);

                    //mon 
                    var user = _applicationDbContext.User!.FirstOrDefault(d => d.Username == model.Username);
                    if ((bool)user!.LDAPAuthentication!)
                        _mailerService.Send(user!.Email!, "EQUICOM SAVINGS BANK Gateway Portal Notification : Create User", MailTemplate.CreateUserAD(data, _encryptionKey), true);
                    else
                        _mailerService.Send(user!.Email!, "EQUICOM SAVINGS BANK Gateway Portal Notification : Create User", MailTemplate.CreateUser(data, _encryptionKey), true);

                    var userRolesList = new List<Model.UserRole>();
                    if (model.UserRoles != null)
                    {
                        userRolesList = model.UserRoles.Select(userRole => new Model.UserRole
                        {

                            UserId = data.Id,
                            RoleId = Convert.ToInt32(StringManipulation.Decrypt(userRole.RoleId!, _encryptionKey)),
                        }).ToList();
                        await _userRoleRepository.AddRangeAsync(userRolesList);
                    }
                    //mon


                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = action,
                        ChangeBy = operatorId!,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = "",
                        NewData = JsonConvert.SerializeObject(data, new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        })
                    };

                    activityLog.Details = string.Format("[{0}] created.", data.Username);

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

        public async Task<Response.Result> Update(Request.User model)
        {
            Response.Result result = new();

            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "EDIT";

                int updateStatus = 0;
                //var @operator = _applicationDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new User() { Id = 0 };
                var operatorId = Convert.ToInt32(model.OpUserId);
                var activityLog = new Model.ActivityLog()
                {
                    UserId = operatorId!,
                    ModuleName = _moduleName,
                    Action = action
                };

                int id = Convert.ToInt32(StringManipulation.Decrypt(model.Id!, _encryptionKey));
                var data = _applicationDbContext.User!.FirstOrDefault(l => l.Id == id);

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
                    var accountTypeChanged = data!.LDAPAuthentication != Convert.ToBoolean(model.LDAPAuthentication);
                    //AccountAuthenticationChanged


                    data!.FirstName = model.FirstName;
                    data!.MiddleName = model.MiddleName;
                    data!.LastName = model.LastName;
                    data!.Email = model.Email;
                    data!.BranchId = Convert.ToInt32(model.Branch);
                    data!.LDAPAuthentication = Convert.ToBoolean(model.LDAPAuthentication);
                    await _repository.UpdateAsync(data);

                    //mon

                    var forDeletion = _applicationDbContext.UserRole!.Where(d => d.UserId == id).ToList();
                    if (forDeletion != null || forDeletion!.Count > 0)
                    {
                        await _userRoleRepository.RemoveRangeAsync(forDeletion);
                    }
                    var userRolesList = new List<Model.UserRole>();
                    if (model.UserRoles != null)
                    {
                        userRolesList = model.UserRoles.Select(userRole => new Model.UserRole
                        {

                            UserId = id,
                            RoleId = Convert.ToInt32(StringManipulation.Decrypt(userRole.RoleId!, _encryptionKey)),
                        }).ToList();
                        await _userRoleRepository.AddRangeAsync(userRolesList);
                    }
                    //mon

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = action,
                        ChangeBy = operatorId!,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = oldValue,
                        NewData = JsonConvert.SerializeObject(data, new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        })
                    };

                    activityLog.Details = string.Format("[{0}] updated.", model.Username);
                    result.Status = "SUCCESS";
                    result.Message = string.Format("{0} updated.", _moduleName);

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(auditLog);

                    if (accountTypeChanged)
                    {
                        if ((bool)data!.LDAPAuthentication)
                            _mailerService.Send(data!.Email!, $"{mailSettings.Subject} : Changed Authentication Type", MailTemplate.AccountAuthenticationChanged(data, "Please use your AD/PC password as your Gateway Application password."), true);
                        else
                            _mailerService.Send(data!.Email!, $"{mailSettings.Subject} : Changed Authentication Type", MailTemplate.AccountAuthenticationChanged(data, String.Format("Please use <b> {0} </b> your as your password.", StringManipulation.Decrypt(data.Password!, _encryptionKey))), true);

                    }
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

        public async Task<Response.Result> Delete(Request.User model)
        {
            Response.Result result = new();
            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var _action = "DELETE";
                //var @operator = _applicationDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new User() { Id = 0 };
                var operatorId = Convert.ToInt32(model.OpUserId);
                var activityLog = new Model.ActivityLog()
                {
                    UserId = operatorId!,
                    ModuleName = _moduleName,
                    Action = _action
                };

                int id = Convert.ToInt32(StringManipulation.Decrypt(model.Id!, _encryptionKey));
                var data = _applicationDbContext.User!.FirstOrDefault(l => l.Id == id);

                if (data != null)
                {
                    var oldValue = JsonConvert.SerializeObject(data!);

                    data.Deleted = true;

                    await _repository.DeleteAsync(data);

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = _action,
                        ChangeBy = operatorId,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = "[deleted :false]",
                        NewData = "[deleted :true]"
                    };

                    activityLog.Details = string.Format("[{0}] deleted.", data.Username);
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

        public async Task<Response.Result> Restore(Request.User model)
        {
            Response.Result result = new();
            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var _action = "RESTORE";
                //var @operator = _applicationDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new User() { Id = 0 };
                var operatorId = Convert.ToInt32(model.OpUserId);
                var activityLog = new Model.ActivityLog()
                {
                    UserId = operatorId!,
                    ModuleName = _moduleName,
                    Action = _action
                };

                int id = Convert.ToInt32(StringManipulation.Decrypt(model.Id!, _encryptionKey));
                var data = _applicationDbContext.User!.FirstOrDefault(l => l.Id == id);

                if (data != null)
                {
                    var oldValue = JsonConvert.SerializeObject(data!);

                    data.Deleted = false;

                    await _repository.DeleteAsync(data);

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = _action,
                        ChangeBy = operatorId!,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = "[deleted :false]",
                        NewData = "[deleted :true]"
                    };

                    activityLog.Details = string.Format("[{0}] restored.", data.Username);
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

        public async Task<Response.Result> ChangeProfileImage(Request.User model)
        {
            Response.Result result = new();

            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var _action = "CHANGEPROFILEIMAGE";
                //var @operator = _applicationDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new User() { Id = 0 };
                var operatorId = Convert.ToInt32(model.OpUserId);
                var activityLog = new Model.ActivityLog()
                {
                    UserId = operatorId!,
                    ModuleName = _moduleName,
                    Action = _action
                };

                int id = Convert.ToInt32(model.Id);
                var user = _applicationDbContext.User!.FirstOrDefault(l => l.Id == id)!;

                if (user != null)
                {
                    var oldValue = EFramework.GetEntityProperties(user!);
                    user.ImageContent = model.ImageContent;
                    user.ImageContentThumbnail = model.ImageContent != null ? ResizeImageToThumbnail(model.ImageContent, 100, 100) : null;
                    user.ImageType = model.ImageType;
                    user.UpdatedBy = operatorId;
                    user.UpdatedDate = DateTime.Now;


                    await _repository.UpdateAsync(user);

                    var auditTrail = new Model.AuditLog()
                    {
                        RecordId = user.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = _action,
                        ChangeBy = operatorId!,
                        ActionDate = (DateTime)user.UpdatedDate,
                        TableName = _moduleName,
                        OriginalData = oldValue,
                        NewData = EFramework.GetEntityProperties(user)
                    };

                    activityLog.Details = string.Format("[user: {0}] profile image updated.", user.Username);
                    result.Status = "SUCCESS";
                    result.Message = string.Format("{0} updated.", _moduleName);

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(auditTrail!);

                    transactionScope.Complete();
                    return result;
                }
                else
                {
                    result.Status = "FAILED";
                    result.Message = string.Format("{0} not exist.", _moduleName);
                }



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
        public async Task<bool> CheckSession(Request.User user)
        {
            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);
            try
            {
                double? sessionTimeoutMinutes = 0;
                if (_systemParameters != null)
                {
                    sessionTimeoutMinutes = _systemParameters.SessionTimeOutMinutes <= 0 ? 3 : _systemParameters.SessionTimeOutMinutes;
                }
                else
                {
                    sessionTimeoutMinutes = 3;
                }
                //var @operator = _applicationDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new User() { Id = 0 };
                var operatorId = Convert.ToInt32(user.OpUserId);
                int id = Convert.ToInt32(user.Id);
                var userActivity = await _applicationDbContext.ActiveUser!
                        .Where(u => u.UserId == id)
                        .GroupBy(u => u.UserId)
                        .Select(g => new
                        {
                            Count = g.Count(),
                            LatestActivity = g.Max(u => u.ActivityDate)
                        })
                        .FirstOrDefaultAsync();

                if (userActivity != null && userActivity.Count > 0)
                {
                    if (DateTime.Now.Subtract((DateTime)userActivity.LatestActivity!).TotalMinutes >= sessionTimeoutMinutes)
                    {
                        return await Task.FromResult(false);
                    }
                    else
                    {
                        return await Task.FromResult(true);
                    }
                }
                else
                {
                    return await Task.FromResult(false);
                }
            }
            catch (TransactionAbortedException ex)
            {
                _logService.LogException(ex, _moduleName);
                return await Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                return await Task.FromResult(false);
            }
            finally
            {
                transactionScope.Dispose();
            }
        }
        public async Task Logout(string userId)
        {
            Response.Result result = new();
            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);
            try
            {
                int id = Convert.ToInt32(userId);
                var activeUser = _applicationDbContext.ActiveUser!.Where(
                        u => u.UserId! == id!).AsQueryable();

                if (activeUser != null)
                {
                    await _activeUserRepository.RemoveRangeAsync(activeUser);
                }

                result.Status = "SUCCESS";
                result.Message = string.Format("{0} deleted", "Active User");
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
        }
        public async Task<Response.Result> ChangePassword(Request.UserPassword model)
        {
            Response.Result result = new();

            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var _action = "CHANGEPASS";

                var activityLog = new Model.ActivityLog()
                {
                    //UserId = model.OpUserId!,
                    ModuleName = _moduleName,
                    Action = _action
                };

                var newPassword = StringManipulation.Encrypt(model.New!, _encryptionKey);
                var user = _applicationDbContext.User!.FirstOrDefault(l => l.Username!.ToString().Trim().ToUpper() == model.Username!.Trim().ToUpper())!;

                if (user != null)
                {
                    activityLog.UserId = user.Id;
                    if (StringManipulation.Decrypt(user.Password!, _encryptionKey) != model.Current)
                    {
                        result.Status = "Invalid";
                        result.Message = @"Invalid current password!";
                        return result;
                    }
                    if (model.New != model.Confirm)
                    {
                        result.Status = "Invalid";
                        result.Message = @"New password does not match with Confirm password!";
                        return result;
                    }
                    //
                    //Standard Security

                    //Check Password History
                    var previousPasswords = _applicationDbContext.PasswordHistory!
                        .Where(p => p.UserId == user.Id)
                        .OrderByDescending(p => p.ChangedDate)
                        .Take((int)_systemParameters.PasswordCycleMaxCount!)!;

                    bool isNewPasswordInvalidDueToHistory = false;
                    if (previousPasswords != null)
                    {
                        isNewPasswordInvalidDueToHistory = previousPasswords.Any(p => p.Password == newPassword);
                    }
                    if (isNewPasswordInvalidDueToHistory)
                    {
                        result.Status = "INVALID";
                        result.Message = String.Format("The password you entered has been used in the last {0} password changes. Please choose a different password that has not been used recently", (int)_systemParameters.PasswordCycleMaxCount);
                        return result;
                    }

                    var oldValue = JsonConvert.SerializeObject(user!, new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });

                    user.Password = newPassword;
                    user.PasswordLastChange = DateTime.Now;
                    user.PasswordExpirationDate = DateTime.Now.AddDays((int)_systemParameters.AccountDormancyDays!); // Add dormancy date as expiration date
                    user.PasswordAttempt = 0;
                    user.Status = 0;
                    user.DefaultPassword = false;
                    user.UpdatedBy = user.Id;
                    user.UpdatedDate = DateTime.Now;

                    await _repository.UpdateAsync(user);


                    //Add Password History

                    await _passwordHistoryRepository.AddAsync(
                        new Model.PasswordHistory
                        {
                            UserId = user.Id,
                            Password = newPassword,
                            ChangedDate = DateTime.Now,
                        });

                    //mailer 
                    _mailerService.Send(user.Email!, $"{mailSettings.Subject} : Unlock User", MailTemplate.ChangePassword(user, _encryptionKey), true);

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = user.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = _action,
                        ChangeBy = user.Id!,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = "",
                        NewData = JsonConvert.SerializeObject(user, new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        })
                    };
                    activityLog.Details = string.Format("[id: {0}] password updated.", user.Id);
                    result.Status = "SUCCESS";
                    result.Message = string.Format("{0} updated.", _moduleName);

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(auditLog!);

                    transactionScope.Complete();
                    return result;
                }
                else
                {
                    result.Status = "FAILED";
                    result.Message = string.Format("{0} not exist.", _moduleName);
                }



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

        public async Task<Response.Result> ResetPassword(Request.User model)
        {
            Response.Result result = new();

            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var _action = "RESETPASS";

                var activityLog = new Model.ActivityLog()
                {
                    ModuleName = _moduleName,
                    Action = _action
                };


                var user = _applicationDbContext.User!.FirstOrDefault(l => l.Username!.ToString().Trim().ToUpper() == model.Username.Trim().ToUpper())!;

                if (user != null)
                {
                    activityLog.UserId = user.Id!;
                    var oldValue = JsonConvert.SerializeObject(user!, new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });
                    var randomPassword = StringManipulation.Random(10);
                    user.Password = StringManipulation.Encrypt(randomPassword, _encryptionKey);
                    user.PasswordExpirationDate = DateTime.Now.AddDays(1);
                    user.Status = 0;
                    user.PasswordAttempt = 0;
                    user.DefaultPassword = true;
                    user.UpdatedBy = user.Id;
                    user.UpdatedDate = DateTime.Now;


                    await _repository.UpdateAsync(user);
                    //mailer 
                    _mailerService.Send(user.Email!, $"{mailSettings.Subject} : Password Reset", MailTemplate.ResetPassword(user, _encryptionKey), true);

                    var auditTrail = new Model.AuditLog()
                    {
                        RecordId = user.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = _action,
                        ChangeBy = user.Id!,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = oldValue,
                        NewData = JsonConvert.SerializeObject(user, new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        })
                    };

                    activityLog.Details = string.Format("[id: {0}] password updated.", user.Id);
                    result.Status = "SUCCESS";
                    result.Message = string.Format("{0} updated.", _moduleName);

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(auditTrail!);

                    transactionScope.Complete();
                    return result;
                }
                else
                {
                    result.Status = "FAILED";
                    result.Message = string.Format("{0} not exist.", _moduleName);
                }



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
        public async Task<Response.Result> UnlockUser(Request.User model)
        {
            Response.Result result = new();

            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var _action = "UNLOCKUSER";

                var activityLog = new Model.ActivityLog()
                {
                    ModuleName = _moduleName,
                    Action = _action
                };


                var user = _applicationDbContext.User!.FirstOrDefault(l => l.Username!.ToString().Trim().ToUpper() == model.Username.Trim().ToUpper())!;

                if (user != null)
                {
                    activityLog.UserId = user.Id;
                    var oldValue = JsonConvert.SerializeObject(user!, new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });
                    user.PasswordAttempt = 0;
                    user.Status = 0;
                    user.UpdatedBy = user.Id;
                    user.UpdatedDate = DateTime.Now;

                    await _repository.UpdateAsync(user);
                    //mailer 
                    _mailerService.Send(user.Email, $"{mailSettings.Subject} : Unlock User", MailTemplate.UnlockUser(user), true);

                    var auditTrail = new Model.AuditLog()
                    {
                        RecordId = user.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = _action,
                        ChangeBy = user.Id!,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = oldValue,
                        NewData = JsonConvert.SerializeObject(user, new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        })
                    };

                    activityLog.Details = string.Format("[id: {0}] password updated.", user.Id);
                    result.Status = "SUCCESS";
                    result.Message = string.Format("{0} updated.", _moduleName);

                    _logService.LogActivity(activityLog);
                    _logService.LogAudit(auditTrail!);

                    transactionScope.Complete();
                    return result;
                }
                else
                {
                    result.Status = "FAILED";
                    result.Message = string.Format("{0} not exist.", _moduleName);
                }



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

        private static byte[] ResizeImageToThumbnail(byte[] imageData, int width, int height)
        {
            using (var ms = new MemoryStream(imageData))
            using (var originalImage = Image.FromStream(ms))
            using (var thumbnail = new Bitmap(width, height))
            using (var graphics = Graphics.FromImage(thumbnail))
            {
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.DrawImage(originalImage, 0, 0, width, height);

                using (var resultStream = new MemoryStream())
                {
                    thumbnail.Save(resultStream, ImageFormat.Jpeg); // Save as JPEG for compression
                    return resultStream.ToArray();
                }
            }
        }
    }
}
