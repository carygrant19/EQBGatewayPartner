using AutoMapper;
using AutoMapper.QueryableExtensions;
using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json; 
using System.Transactions;
using Model = Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services
{
    public class ModuleService(IConfiguration configuration, EFDbContext efDbContext, IMapper mapper, IRepository<Model.Module> repository, IRepository<Model.ModulePermission> mpRepository, ILogService logService) : IModuleService
    {
        private readonly EFDbContext _efDbContext = efDbContext;
        private readonly IRepository<Model.Module> _repository = repository;
        private readonly IRepository<Model.ModulePermission> _mpRepository = mpRepository;
        private readonly ILogService _logService = logService;
        private readonly IMapper _mapper = mapper;
        private readonly string _encryptionKey = configuration["AppContext:EncryptionKey"]!;
        private readonly string _moduleName = "Module";

        public async Task<List<Response.Module>> GetModuleGroup()
        {
            var result = new List<Response.Module>();
            try
            {
                var groups = _efDbContext.Module!
                    .Where(r => r.Deleted != true
                    && r.ParentId == 0
                    && r.Code.ToUpper() != "HOME")
                    .OrderBy(r => r.DisplayOrder)
                    .AsQueryable();

                result = (from m in groups
                          select new Response.Module
                          {
                              Id = m.Id.ToString(),
                              Code = m.Code!,
                              Name = m.Name!,
                              ModuleType = m.ModuleType!,
                              ParentId = m.ParentId.ToString()!,
                              ParentName = m.Parent!.Code!,
                              Description = m.Description!,
                              DisplayOrder = m.DisplayOrder!.ToString()!,
                              Url = m.Url!,
                              Icon = m.Icon!,
                              AuditContent = m.AuditContent!,
                              Show = m.Show,
                              Path = m.Id.ToString()
                          }
                            ).ToList();


            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
            }
            return await Task.FromResult(result);
        }


        public async Task<List<Response.Permission>> GetAllPermission()
        {
            try
            {

                var moduleActionList = _efDbContext.Set<Model.Permission>().AsQueryable()
                    .Where(e => e.Deleted != true).OrderBy(e => e.Code);

                var result = (from a in moduleActionList
                              select new Response.Permission
                              {
                                  Code = a.Code,
                                  Description = a.Description!
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
        public async Task<List<Response.ModuleAccess>> GetAllParent()
        {
            try
            {
                var moduleList = _efDbContext.Set<Model.Module>()
                    .Include(m => m.ModulePermission)
                    .AsQueryable();

                var rootModules = await moduleList
                    .Where(m => m.ParentId == 0 && !m.Deleted)
                    .OrderBy(m => m.DisplayOrder)
                    .Select(m => new Response.ModuleAccess
                    {
                        Id = m.Id.ToString(),
                        Code = m.Code!,
                        Name = m.Name!,
                        ModuleType = m.ModuleType!,
                        ParentId = m.ParentId.ToString()!,
                        ParentName = string.Empty,
                        Description = m.Description!,
                        DisplayOrder = m.DisplayOrder!.ToString()!,
                        Url = m.Url!,
                        Icon = m.Icon!,
                        Permissions = string.Join('|', m.ModulePermission!.Select(r => r.PermissionId).ToList()),
                        AuditContent = m.AuditContent!,
                        Show = m.Show,
                        Path = m.Id.ToString(),
                        ModuleGroup = m.Code!, // Root’s own Code
                        ModuleGroupDesc = m.Name!, // Root’s Name (or Description)
                        ModuleGroupDisplayOrder = (int)m.DisplayOrder
                    })
                    .ToListAsync();

                var allModules = moduleList
                    .Where(m => !m.Deleted)
                    .OrderBy(m => m.DisplayOrder)
                    .ToList();

                var moduleMap = allModules.ToDictionary(m => m.Id);

                List<Response.ModuleAccess> result = [];
                int rowNumber = 1;

                foreach (var rootModule in rootModules)
                {
                    PopulateChildren(rootModule, moduleMap, result, rootModule.Path, rootModule.Name, rootModule.Code, rootModule.Name, rootModule.ModuleGroupDisplayOrder, true);
                    rowNumber++;
                }

                // ✅ THIS IS THE CORRECT ORDERING PLACE
                return result
                    .OrderBy(r => r.ModuleGroupDisplayOrder)
                    .ThenBy(r => r, Comparer<Response.ModuleAccess>.Create((x, y) => ComparePath(x.Path, y.Path)))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                return [];
            }
        }


        private static void PopulateChildren(
            Response.ModuleAccess parentModule,
            Dictionary<int, Model.Module> moduleMap,
            List<Response.ModuleAccess> result,
            string currentPath,
            string parentName,
            string topParentCode,
            string topParentName,
            int topParentDisplayOrder,
            bool isParentList)
        {
            result.Add(parentModule);

            var children = moduleMap.Values
                .Where(m => m.ParentId.ToString() == parentModule.Id)
                .OrderBy(m => m.DisplayOrder)
                .ToList();

            int rowNumber = 1;
            foreach (var child in children)
            {
                var childModuleDAL = new Response.ModuleAccess
                {
                    Id = child.Id.ToString(),
                    Code = child.Code!,
                    Name = parentName + " > " + child.Name!, // Construct the name
                    ModuleType = child.ModuleType!,
                    ParentId = child.ParentId!.ToString()!,
                    ParentName = child.Parent != null ? child.Parent.Code! : string.Empty,
                    Description = child.Description!,
                    DisplayOrder = child.DisplayOrder?.ToString() ?? string.Empty,
                    Url = child.Url!,
                    Icon = child.Icon!,
                    Permissions = string.Join('|', child.ModulePermission!.Select(r => r.PermissionId).ToList()),
                    AuditContent = child.AuditContent!,
                    Show = child.Show,
                    ModuleGroup = topParentCode,       // 🟢 same topmost Code
                    ModuleGroupDesc = topParentName,
                    ModuleGroupDisplayOrder = topParentDisplayOrder // 🟢 same topmost Name
                };

                // Build hierarchical path
                string paddedRowNumber = rowNumber.ToString().PadLeft(4, '0');
                childModuleDAL.Path = $"{currentPath}-{paddedRowNumber}";

                // Continue recursion
                PopulateChildren(childModuleDAL, moduleMap, result, childModuleDAL.Path, childModuleDAL.Name, topParentCode, topParentName, topParentDisplayOrder, isParentList);
                rowNumber++;
            }
        }
        private static List<int> ParsePath(string path)
        {
            return path
                .Split('-', StringSplitOptions.RemoveEmptyEntries)
                .Select(s =>
                {
                    // Handle possible non-numeric segments safely
                    return int.TryParse(s, out int num) ? num : int.MaxValue;
                })
                .ToList();
        }

        private static int ComparePath(string pathA, string pathB)
        {
            var aParts = ParsePath(pathA);
            var bParts = ParsePath(pathB);
            int max = Math.Max(aParts.Count, bParts.Count);

            for (int i = 0; i < max; i++)
            {
                int a = i < aParts.Count ? aParts[i] : 0;
                int b = i < bParts.Count ? bParts[i] : 0;

                if (a != b)
                    return a.CompareTo(b);
            }

            return 0;
        }

        public async Task<List<Response.ModuleAccess>> GetUserModuleAccess(string roleId)
        {
            try
            {
                var moduleList = _efDbContext.Set<Model.RoleModulePermission>()
                    .Where(rm => rm.RoleId == Convert.ToInt32(roleId) && rm.Module!.Deleted != true)
                    .Include(m => m.Module)
                    .OrderBy(m => m.Module!.DisplayOrder)
                    .AsQueryable();
                var result = await (from m in moduleList
                                    select new Response.ModuleAccess
                                    {
                                        Id = m.Module!.Id.ToString(),
                                        Code = m.Module.Code!,
                                        Name = m.Module.Name!,
                                        ModuleType = m.Module.ModuleType!,
                                        ParentId = m.Module.ParentId.ToString()!,
                                        ParentName = m.Module.Parent!.Code!,
                                        Description = m.Module.Description!,
                                        DisplayOrder = m.Module.DisplayOrder.ToString()!,
                                        Url = m.Module.Url!,
                                        Icon = m.Module.Icon!,
                                        AuditContent = m.Module.AuditContent!,
                                        Show = m.Module.Show,
                                        Path = m.Id.ToString()
                                    }).ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                return [];
            }
        }
        public async Task<List<Response.Module>> Get()
        {
            try
            {
                var result = _efDbContext.Set<Model.Module>()
                    .Where(e => e.Deleted != true).ProjectTo<Response.Module>(_mapper.ConfigurationProvider)!.ToList();

                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                return [];
            }
        }

        public async Task<Response.VModule> Filter(Request.FParam model)
        {
            try
            {
                var propertySelector = EFramework.BuildPropertySelector<Model.Module>(model.SortColumn);

                Response.VModule vData = new();

                var query = _efDbContext.Module!.Include(p => p.Parent).Include(m => m.ModulePermission).AsQueryable();

                //if (model.Filters != null && model.Filters.Count != 0)
                //    query = query.Where(ExpressionBuilder.GetExpression<Model.Module>(model.Filters)!);

                if (model.Filters != null && model.Filters.Count != 0)
                {
                    query = query.Where(u => u.Description.Contains(Convert.ToString(model.Filters[0].Value))
                    || u.Code.Contains(Convert.ToString(model.Filters[0].Value))
                    || u.Name.Contains(Convert.ToString(model.Filters[0].Value)));
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

                vData.Data = _mapper.Map<List<Response.FModule>>(pagedQuery.ToList());

                return await Task.FromResult(vData);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                throw;
            }
        }
        //RAMON END
        public async Task<Response.Result> Create(Request.Module model)
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

                var data = _efDbContext.Module!.FirstOrDefault(d => d.Code == model.Code);

                if (data == null)
                {

                    data = new()
                    {
                        Code = model.Code.Trim().ToUpper(),
                        Name = model.Name,
                        ModuleType = model.ModuleType,
                        DisplayOrder = model.DisplayOrder,
                        ParentId = model.ParentId == "" ? 0 : Convert.ToInt32(model.ParentId),
                        Description = model.Description,
                        Url = model.Url,
                        Icon = model.Icon,
                        AuditContent = model.AuditContent,
                        Show = model.Show,
                    };

                    var permissions = model.Permission.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    var permission = new List<Model.ModulePermission>();

                    await _repository.AddAsync(data);

                    if (permissions.Length != 0)
                    {
                        permission = permissions.Select(p => new Model.ModulePermission
                        {
                            ModuleId = data.Id!,
                            PermissionId = Convert.ToInt32(p)!
                        }).ToList();

                    }


                    await _mpRepository.AddRangeAsync(permission);

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

        public async Task<Response.Result> Update(Request.Module model)
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
                var data = _efDbContext.Module!.FirstOrDefault(l => l.Id == id);

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

                    data!.Code = model.Code;
                    data.Name = model.Name;
                    data.ModuleType = model.ModuleType;
                    data.DisplayOrder = model.DisplayOrder;
                    data.ParentId = model.ParentId == "" ? 0 : Convert.ToInt32(model.ParentId);
                    data.Description = model.Description;
                    data.Url = model.Url;
                    data.Icon = model.Icon;
                    data.AuditContent = model.AuditContent;
                    data.Show = model.Show;


                    var permissions = model.Permission.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    var permission = new List<Model.ModulePermission>();

                    if (permissions.Length != 0)
                    {
                        permission = permissions.Select(p => new Model.ModulePermission
                        {
                            ModuleId = Convert.ToInt32(StringManipulation.Decrypt(model.Id, _encryptionKey))!,
                            PermissionId = Convert.ToInt32(p)!,
                        }).ToList();

                    }

                    await _repository.UpdateAsync(data);
                    await _mpRepository.DeleteByConditionAsync(m => m.ModuleId == Convert.ToInt32(StringManipulation.Decrypt(model.Id, _encryptionKey)));
                    await _mpRepository.UpdateRangeAsync(permission);

                    var auditLog = new Model.AuditLog()
                    {
                        RecordId = data.Id.ToString(),
                        Terminal = model.Terminal!,
                        OperationType = action,
                        //ChangeBy = operatorId!,
                        ChangeBy = user.Id!,
                        ActionDate = DateTime.Now,
                        TableName = _moduleName,
                        OriginalData = oldValue,
                        NewData = JsonConvert.SerializeObject(data, new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        })
                    };

                    activityLog.Details = string.Format("[{0}] updated.", model.Code);
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
        public async Task<Response.Result> Delete(Request.Module model)
        {
            Response.Result result = new();
            TransactionScope transactionScope = new(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                var action = "DELETE";
                var user = _efDbContext.User!.FirstOrDefault(d => d.Username == model.OpUser) ?? new Model.User() { Id = 0 };

                var activityLog = new Model.ActivityLog()
                {
                    //UserId = operatorId!,
                    UserId = user.Id,
                    ModuleName = _moduleName,
                    Action = action
                };

                int id = Convert.ToInt32(StringManipulation.Decrypt(model.Id!, _encryptionKey));
                var data = _efDbContext.Module!.FirstOrDefault(l => l.Id == id);

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
        public async Task<Response.Result> Restore(Request.Module model)
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
                var data = _efDbContext.Module!.FirstOrDefault(l => l.Id == id);

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
