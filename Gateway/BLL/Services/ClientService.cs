using AutoMapper;
using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Transactions;
using Models = Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services
{
    public class ClientService(
        EFDbContext efDbContext,
        IConfiguration configuration,
        IMapper mapper,
        IRepository<Models.Client> clientRepository,
        ILogService logService) : IClientService
    {
        private readonly EFDbContext _efDbContext = efDbContext;
        private readonly IRepository<Models.Client> _clientRepository = clientRepository;
        private readonly ILogService _logService = logService;
        private readonly IMapper _mapper = mapper;
        private readonly string _moduleName = "Client";
        private readonly string _encryptionKey = configuration["AppContext:EncryptionKey"]!;

        public async Task<Response.Client> Authenticate(string username, string password)
        {
            return await ByUsernameAndPassword(username, password);
        }

        public async Task<Response.Client> ByApiKey(string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key)) return new();

                var result = await _efDbContext.Client
                    .Include(c => c.Company)
                    .Include(c => c.Credentials)
                    .FirstOrDefaultAsync(c => !c.Deleted
                        && c.Status == "Active"
                        && c.Credentials.Any(cr => cr.ApiKey.ToUpper() == key.ToUpper() && cr.IsActive));

                return _mapper.Map<Response.Client>(result);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }

        public async Task<Response.Client> ByUsernameAndPassword(string username, string password)
        {
            // Sa B2B architecture, ang 'username' ay Client Code at ang 'password' ay API Key/Secret
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return new();

                var result = await _efDbContext.Client
                    .Include(c => c.Company)
                    .Include(c => c.Credentials)
                    .FirstOrDefaultAsync(c => c.Code.ToUpper() == username.ToUpper()
                        && !c.Deleted
                        && c.Status == "Active"
                        && c.Credentials.Any(cr => (cr.ApiKey == password || cr.ApiSecretHash == password) && cr.IsActive));

                return _mapper.Map<Response.Client>(result);
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
                var query = _efDbContext.Client
                    .Include(c => c.Company)
                    .Include(c => c.Credentials)
                    .AsQueryable();

                if (!includeDeleted)
                    query = query.Where(e => !e.Deleted);

                var clientList = await query.ToListAsync();

                var result = clientList.Select(u => new Response.Client
                {
                    Id = u.Id.ToString(),
                    Code = u.Code,
                    Name = u.Name,
                    CompanyId = u.CompanyId.ToString(),
                    CompanyCode = u.Company?.Code ?? string.Empty,
                    CompanyDescription = u.Company?.Description ?? string.Empty,
                    Description = u.Description ?? string.Empty,
                    Status = u.Status,
                    ApiKey = u.Credentials.FirstOrDefault(cr => cr.IsActive)?.ApiKey ?? string.Empty,
                    SSLRequired = u.SSLRequired,
                    Deleted = u.Deleted
                }).ToList();

                return result;
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
                if (!int.TryParse(id, out int clientId)) return new();

                var result = await _efDbContext.Client
                    .Include(c => c.Company)
                    .Include(c => c.Credentials)
                    .FirstOrDefaultAsync(b => b.Id == clientId && !b.Deleted);

                return _mapper.Map<Response.Client>(result);
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

                var clientList = _efDbContext.Client
                    .Include(c => c.Company)
                    .Include(c => c.Credentials)
                    .AsQueryable();

                if (model.Filters != null && model.Filters.Count != 0)
                    clientList = clientList.Where(ExpressionBuilder.GetExpression<Models.Client>(model.Filters));

                clientList = model.Descending
                    ? clientList.OrderByDescending(propertySelector)
                    : clientList.OrderBy(propertySelector);

                data.CurrentPage = model.PageNum;
                data.TotalRecord = await clientList.CountAsync();
                data.TotalPage = (int)Math.Ceiling((double)data.TotalRecord / model.PageSize);

                int recordsToSkip = (model.PageNum - 1) * model.PageSize;
                var pagedQuery = await clientList.Skip(recordsToSkip).Take(model.PageSize).ToListAsync();

                data.Data = pagedQuery.Select(u => new Response.FClient
                {
                    Id = u.Id.ToString(),
                    Code = u.Code,
                    Name = u.Name,
                    CompanyId = u.CompanyId.ToString(),
                    CompanyCode = u.Company?.Code ?? string.Empty,
                    CompanyDescription = u.Company?.Description ?? string.Empty,
                    Description = u.Description ?? string.Empty,
                    Status = u.Status,
                    ApiKey = u.Credentials.FirstOrDefault(cr => cr.IsActive)?.ApiKey ?? string.Empty,
                    SSLRequired = u.SSLRequired,
                    Deleted = u.Deleted
                }).ToList();

                return data;
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
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "ADD";
                var user = await _efDbContext.User.FirstOrDefaultAsync(u => u.Username == model.OpUser);
                int userId = user?.Id ?? 0;

                var exists = await _efDbContext.Client.AnyAsync(u => u.Code == model.Code);

                if (!exists)
                {
                    var data = new Models.Client
                    {
                        Code = model.Code,
                        Name = model.Name,
                        CompanyId = Convert.ToInt32(model.CompanyId),
                        Description = model.Description ?? string.Empty,
                        Status = "Active",
                        SSLRequired = model.SSLRequired,
                        CreatedBy = userId,
                        CreatedDate = DateTime.Now
                    };

                    await _clientRepository.AddAsync(data);
                    await _efDbContext.SaveChangesAsync();

                    // Otomatikong gawa ng Primary API Credential
                    var credential = new Models.ClientCredential
                    {
                        ClientId = data.Id,
                        KeyType = "Primary",
                        ApiKey = StringManipulation.Random(16),
                        ApiSecretHash = StringManipulation.Random(16),
                        IsActive = true
                    };

                    _efDbContext.Set<Models.ClientCredential>().Add(credential);
                    await _efDbContext.SaveChangesAsync();

                    var auditLog = new Models.AuditLog
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal ?? "SYSTEM",
                        OperationType = action,
                        ChangeBy = userId,
                        ActionDate = data.CreatedDate ?? DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = "",
                        NewData = JsonConvert.SerializeObject(data, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })
                    };

                    _logService.LogActivity(new Models.ActivityLog { UserId = userId, ModuleName = _moduleName, Action = action, Details = $"[Client Code: {data.Code}] created." });
                    _logService.LogAudit(auditLog);

                    result = new Response.Result { Status = "SUCCESS", Message = $"{_moduleName} created successfully." };
                }
                else
                {
                    result = new Response.Result { Status = "FAILED", Message = $"Client Code: {model.Code} already exists." };
                }

                transactionScope.Complete();
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                result = new Response.Result { Status = "ERROR", Message = "Error encountered" };
            }

            return result;
        }

        public async Task<Response.Result> Update(Request.Client model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "EDIT";
                var user = await _efDbContext.User.FirstOrDefaultAsync(u => u.Username == model.OpUser);
                int userId = user?.Id ?? 0;

                if (!int.TryParse(model.Id, out int clientId))
                    return new Response.Result { Status = "FAILED", Message = $"{_moduleName} invalid ID." };

                var data = await _efDbContext.Client.FirstOrDefaultAsync(u => u.Id == clientId);

                if (data != null)
                {
                    var isCodeTaken = await _efDbContext.Client.AnyAsync(d => d.Code == model.Code && d.Id != clientId);
                    if (isCodeTaken)
                    {
                        return new Response.Result { Status = "FAILED", Message = $"{_moduleName} [Code: {model.Code}] already exists." };
                    }

                    var oldValue = JsonConvert.SerializeObject(data, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

                    data.Code = model.Code;
                    data.Name = model.Name;
                    data.CompanyId = Convert.ToInt32(model.CompanyId);
                    data.Description = model.Description ?? string.Empty;
                    data.SSLRequired = model.SSLRequired;
                    data.UpdatedBy = userId;
                    data.UpdatedDate = DateTime.Now;

                    await _clientRepository.UpdateAsync(data);
                    await _efDbContext.SaveChangesAsync();

                    var auditLog = new Models.AuditLog
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal ?? "SYSTEM",
                        OperationType = action,
                        ChangeBy = userId,
                        ActionDate = data.UpdatedDate ?? DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = oldValue,
                        NewData = JsonConvert.SerializeObject(data, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })
                    };

                    _logService.LogActivity(new Models.ActivityLog { UserId = userId, ModuleName = _moduleName, Action = action, Details = $"[Code: {model.Code}] updated." });
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
                result = new Response.Result { Status = "ERROR", Message = "Error encountered" };
            }

            return result;
        }

        public async Task<Response.Result> Delete(Request.Client model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "DELETE";
                var user = await _efDbContext.User.FirstOrDefaultAsync(u => u.Username == model.OpUser);
                int userId = user?.Id ?? 0;

                var data = await _efDbContext.Client.FirstOrDefaultAsync(u => u.Code == model.Code);

                if (data != null)
                {
                    data.Deleted = true;
                    data.UpdatedBy = userId;
                    data.UpdatedDate = DateTime.Now;

                    await _clientRepository.UpdateAsync(data);
                    await _efDbContext.SaveChangesAsync();

                    var auditLog = new Models.AuditLog
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal ?? "SYSTEM",
                        OperationType = action,
                        ChangeBy = userId,
                        ActionDate = data.UpdatedDate ?? DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = "[deleted: false]",
                        NewData = "[deleted: true]"
                    };

                    _logService.LogActivity(new Models.ActivityLog { UserId = userId, ModuleName = _moduleName, Action = action, Details = $"[Code: {data.Code}] deleted." });
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
                result = new Response.Result { Status = "ERROR", Message = "Error encountered" };
            }

            return result;
        }

        public async Task<Response.Result> Restore(Request.Client model)
        {
            Response.Result result = new();
            using TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "RESTORE";
                var user = await _efDbContext.User.FirstOrDefaultAsync(u => u.Username == model.OpUser);
                int userId = user?.Id ?? 0;

                var data = await _efDbContext.Client.FirstOrDefaultAsync(u => u.Code == model.Code);

                if (data != null)
                {
                    data.Deleted = false;
                    data.UpdatedBy = userId;
                    data.UpdatedDate = DateTime.Now;

                    await _clientRepository.UpdateAsync(data);
                    await _efDbContext.SaveChangesAsync();

                    var auditLog = new Models.AuditLog
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal ?? "SYSTEM",
                        OperationType = action,
                        ChangeBy = userId,
                        ActionDate = data.UpdatedDate ?? DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = "[deleted: true]",
                        NewData = "[deleted: false]"
                    };

                    _logService.LogActivity(new Models.ActivityLog { UserId = userId, ModuleName = _moduleName, Action = action, Details = $"[Code: {data.Code}] restored." });
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
                result = new Response.Result { Status = "ERROR", Message = "Error encountered" };
            }

            return result;
        }

        public async Task<Response.APISecurityResult> ResetAPIKeySecret(Request.Client model)
        {
            Response.APISecurityResult result = new();
            string newKey = StringManipulation.Random(16);
            string newSecret = StringManipulation.Random(16);

            try
            {
                if (!int.TryParse(model.Id, out int clientId)) return new();

                var credential = await _efDbContext.Set<Models.ClientCredential>()
                    .FirstOrDefaultAsync(b => b.ClientId == clientId && b.KeyType == "Primary");

                if (credential != null)
                {
                    credential.ApiKey = newKey;
                    credential.ApiSecretHash = newSecret;
                    _efDbContext.Set<Models.ClientCredential>().Update(credential);
                    await _efDbContext.SaveChangesAsync();

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
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                result.Status = "ERROR";
                result.Key = "";
                result.Secret = "";
            }

            return result;
        }

        public async Task<Response.APISecurityResult> ResetPassword(Request.Client model)
        {
            // B2B Alias para sa Reset Secret
            return await ResetAPIKeySecret(model);
        }
    }
}