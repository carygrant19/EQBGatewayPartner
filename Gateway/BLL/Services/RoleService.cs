using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Helper; 
using System.Transactions;
using Model = Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;
using Gateway.BLL.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace Gateway.BLL.Services
{
    public class RoleService(IConfiguration configuration, EFDbContext efDbContext, IRepository<Model.Role> repository, IRepository<Model.RoleModulePermission> roleModulePermissionRepository, ILogService logService) : IRoleService
    {
        private readonly EFDbContext _efDbContext = efDbContext;
        private readonly IRepository<Model.Role> _repository = repository;
        private readonly IRepository<Model.RoleModulePermission> _roleModulePermissionRepository = roleModulePermissionRepository;
        private readonly ILogService _logService = logService; 
        private readonly string _encryptionKey = configuration["AppContext:EncryptionKey"]!;
        private readonly string _moduleName = "Role";

        public async Task<List<Response.RoleModule>> GetRoleModules(string username)
        {
            try
            {
                // Get the user
                var user = await _efDbContext.User!
                    .FirstOrDefaultAsync(d => d.Username!.ToUpper() == username.ToUpper());

                if (user == null)
                {
                    return []; // Return empty if user not found
                }

                // Get all Role IDs assigned to the user
                var roleCodes = await _efDbContext.UserRole!
                    .Where(r => r.UserId == user.Id)
                    .Select(r => r.RoleId)
                    .ToListAsync();

                if (!roleCodes.Any())
                {
                    return []; // Return empty if no roles assigned
                }

                // Fetch RoleModulePermissions with an explicit LINQ Left Join to bypass the EF Inner Join trap
                var queryResult = await (from rmp in _efDbContext.RoleModulePermission!
                                         join p in _efDbContext.Set<Model.Permission>() on rmp.PermissionId equals p.Id into permGroup
                                         from p in permGroup.DefaultIfEmpty() // <--- This forces a SQL LEFT JOIN
                                         where roleCodes.Contains(rmp.RoleId)
                                         where _efDbContext.ModulePermission!
                                             .Any(mp => mp.ModuleId == rmp.ModuleId && mp.PermissionId == rmp.PermissionId) || rmp.PermissionId == 0
                                         select new { rmp, p })
                                         .ToListAsync();

                // Reconstruct the objects into a list of RoleModulePermission so the rest of your code runs unchanged
                var validRoleModulePermissions = queryResult.Select(x =>
                {
                    x.rmp.Permissions = x.p; // Will be safely null if PermissionId is 0
                    return x.rmp;
                }).ToList();

                // Get unique valid Module IDs from RoleModulePermissions
                var validModuleIds = validRoleModulePermissions
                    .Select(rmp => rmp.ModuleId)
                    .Distinct()
                    .ToList();

                // Fetch only modules that exist in validRoleModulePermissions AND are not deleted
                var baseModules = await _efDbContext.Module!
                    .Where(m => !m.Deleted && validModuleIds.Contains(m.Id))
                    .ToListAsync();

                // Process modules & permissions
                var responseModules = baseModules.Select(module => new Response.RoleModule
                {
                    Id = module.Id.ToString(),
                    Code = module.Code,
                    Name = module.Name,
                    ModuleType = module.Id.ToString(),
                    ParentId = module.ParentId?.ToString() ?? "0",
                    DisplayOrder = module.DisplayOrder.ToString(),
                    Url = module.Url,
                    Icon = module.Icon,
                    Show = module.Show,
                    Path = module.Id.ToString(),
                    Description = module.Description,

                    // Process permissions
                    Permissions = string.Join("|", validRoleModulePermissions
                        .Where(rmp => rmp.ModuleId == module.Id)
                        .Select(rmp => $"[{(rmp.Permissions == null ? string.Empty : rmp.Permissions.Code)}]")
                        .DefaultIfEmpty("[]") // Default to "[0]" if no valid permissions exist
                    )
                }).ToList();

                // Create hierarchy
                var result = new List<Response.RoleModule>();
                foreach (var module in responseModules.Where(m => m.ParentId == "0").OrderBy(m => int.Parse(m.DisplayOrder)))
                {
                    BuildModuleHierarchy(module, responseModules, result, module.Path);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, "GetRoleModules");
                return [];
            }
        }

        private static void BuildModuleHierarchy(Response.RoleModule module, List<Response.RoleModule> allModules, List<Response.RoleModule> result, string parentPath)
        {
            // Update the path to reflect the full hierarchy
            module.Path = string.IsNullOrEmpty(parentPath) ? module.Id : $"{parentPath}/{module.Id}";

            // Add the module to the result
            result.Add(module);

            // Find children of the current module and sort them by DisplayOrder
            var children = allModules
                .Where(m => m.ParentId == module.Id)
                .OrderBy(m => int.TryParse(m.DisplayOrder, out var order) ? order : int.MaxValue)
                .ToList();

            // Recursively build the hierarchy for each child
            foreach (var child in children)
            {
                BuildModuleHierarchy(child, allModules, result, module.Path); // Pass the updated path to children
            } 
        }
        public async Task<Gateway.BLL.DTO.Response.VRole> Get()
        {
            try
            { 
                var roles = await _efDbContext.Set<Model.Role>()
                    .Where(e => e.Deleted != true)
                    .OrderBy(r => r.Description)
                    .Select(r => new Gateway.BLL.DTO.Response.FRole
                    {
                        Id = r.Id.ToString(),
                        Code = r.Code,
                        Description = r.Description,
                        Deleted = r.Deleted
                                                     
                    })
                    .ToListAsync();
                 
                return new Gateway.BLL.DTO.Response.VRole
                {
                    Data = roles
                };
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }

        public async Task<Response.VRole> Filter(Request.FParam model)
        {
            try
            {
                var propertySelector = EFramework.BuildPropertySelector<Model.Role>(model.SortColumn);

                Response.VRole vData = new();

                var query = _efDbContext.Role!.AsQueryable();

                //if (model.Filters != null && model.Filters.Count != 0)
                //query = query.Where(ExpressionBuilder.GetExpression<Model.Role>(model.Filters)!);


                if (model.Filters != null && model.Filters.Count != 0)
                {
                    query = query.Where(u => u.Description.Contains(Convert.ToString(model.Filters[0].Value)) || u.Code.Contains(Convert.ToString(model.Filters[0].Value)));
                }

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
                              select new Response.FRole
                              {
                                  Id = u.Id.ToString(),
                                  Code = u.Code,
                                  Description = u.Description,

                              }).ToList();

                vData.Data = result;

                return await Task.FromResult(vData);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }
        public async Task<List<Response.Module>> ById(string id)
        {
            try
            {
                var query = _efDbContext.Set<Model.RoleModulePermission>()!.Where(b => b.RoleId == Convert.ToInt32(id)).AsQueryable();
                var modules = _efDbContext.Module!.Where(m => m.Deleted != true).AsQueryable();

                var result = (from q in query
                              join m in modules on q.ModuleId equals m.Id
                              group new { q, m } by new { q.ModuleId, q.RoleId } into g
                              select new Response.Module
                              {
                                  Id = g.First().m.Id.ToString(),
                                  Code = Convert.ToString(g.Key.ModuleId),
                                  Name = g.First().m.Name!,
                                  ModuleType = g.First().m.ModuleType!,
                                  ParentId = g.First().m.ParentId.ToString()!,
                                  ModulePermission = string.Join('|', g.Select(x => x.q.PermissionId))
                              }
                ).ToList();
                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                return [];
            }

        }
       

        public async Task<Response.Result> Create(Request.Role model)
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
                //int id = Convert.ToInt32(StringManipulation.Decrypt(model.Id!, _encryptionKey));
                var data = _efDbContext.Role!.FirstOrDefault(d => d.Code.Trim().ToUpper() == model.Code.Trim().ToUpper());

                if (data == null)
                {

                    data = new()
                    {
                        Code = model.Code.Trim().ToUpper(),
                        Description = model.Description,
                    };

                    await _repository.AddAsync(data);
                    //Delete existing RoleModuleAccess
                    //await _roleModulePermissionRepository.DeleteByConditionAsync(r => r.RoleId == id);

                    //Add new RoleModuleAccess
                    if (model.RoleModulePermission != null)
                    {
                        var rmp = new List<Model.RoleModulePermission>();
                        for (int x = 0; x < model.RoleModulePermission.Count; x++)
                        {
                            if (model.RoleModulePermission[x].PermissionId != null)
                            {
                                for (int y = 0; y < model.RoleModulePermission[x].PermissionId!.Length; y++)
                                {
                                    rmp.Add(new Model.RoleModulePermission
                                    {
                                        RoleId = data.Id,
                                        ModuleId = Convert.ToInt32(model.RoleModulePermission[x].ModuleId),
                                        PermissionId = Convert.ToInt32(model.RoleModulePermission[x].PermissionId![y])
                                    });
                                }
                            }
                            else
                            {
                                rmp.Add(new Model.RoleModulePermission
                                {
                                    RoleId = data.Id,
                                    ModuleId = Convert.ToInt32(model.RoleModulePermission[x].ModuleId),
                                    PermissionId = 0
                                });
                            }

                        }
                        await _roleModulePermissionRepository.AddRangeAsync(rmp);
                    }

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

        public async Task<Response.Result> Update(Request.Role model)
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
                var data = _efDbContext.Role!.FirstOrDefault(l => l.Id == id);

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

                    data!.Code = model.Code.Trim();
                    data!.Description = model.Description;

                    await _repository.UpdateAsync(data);
                    //Delete existing RoleModuleAccess
                    await _roleModulePermissionRepository.DeleteByConditionAsync(r => r.RoleId == id);

                    //Add new RoleModuleAccess
                    if (model.RoleModulePermission != null)
                    {
                        var rmp = new List<Model.RoleModulePermission>();
                        for (int x = 0; x < model.RoleModulePermission.Count; x++)
                        {
                            if (model.RoleModulePermission[x].PermissionId != null)
                            {
                                for (int y = 0; y < model.RoleModulePermission[x].PermissionId!.Length; y++)
                                {
                                    rmp.Add(new Model.RoleModulePermission
                                    {
                                        RoleId = id,
                                        ModuleId = Convert.ToInt32(model.RoleModulePermission[x].ModuleId),
                                        PermissionId = Convert.ToInt32(model.RoleModulePermission[x].PermissionId![y])
                                    });
                                }
                            }
                            else
                            {
                                rmp.Add(new Model.RoleModulePermission
                                {
                                    RoleId = id,
                                    ModuleId = Convert.ToInt32(model.RoleModulePermission[x].ModuleId),
                                    PermissionId = 0
                                });
                            }

                        }
                        await _roleModulePermissionRepository.AddRangeAsync(rmp);
                    }

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
        public async Task<Response.Result> Delete(Request.Role model)
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
                var data = _efDbContext.Role!.FirstOrDefault(l => l.Id == id);

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
        public async Task<Response.Result> Restore(Request.Role model)
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
                var data = _efDbContext.Role!.FirstOrDefault(l => l.Id == id);

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
