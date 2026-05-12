using Models = Gateway.Data.Models;
namespace Gateway.Helper
{
    public interface ILogService
    {
        void LogActivity(Models.ActivityLog model);
        void LogAudit(Models.AuditLog model);
        void LogException(Exception ex, string moduleName);
    }

    public class LogService(EFDbContext efDbContext) : ILogService
    {
        private readonly EFDbContext _efDbContext = efDbContext;

        public void LogActivity(Models.ActivityLog model)
        {
            try
            {
                _efDbContext.ActivityLog!.Add(model);
                _efDbContext.SaveChanges();
            }
            catch
            {
                throw;
            }
        }
        public void LogAudit(Models.AuditLog model)
        {
            try
            {
                _efDbContext.AuditLog!.Add(model);
                _efDbContext.SaveChanges();
            }
            catch
            {
                throw;
            }
        }
        public void LogException(Exception ex, string moduleName)
        {
            try
            {
                Models.ExceptionLog model = new()
                {
                    ModuleName = moduleName,
                    Message = ex.Message.ToString(),
                    Source = ex.Source!.ToString(),
                    InnerException = (ex.InnerException! == null ? "" : ex.InnerException!.ToString().Replace("'", "''")),
                    StackTrace = (ex.StackTrace! == null ? "" : ex.StackTrace!.ToString()),
                    LogDate = DateTime.Now
                };

                _efDbContext.ExceptionLog!.Add(model);
                _efDbContext.SaveChanges();
            }
            catch
            {
                throw;
            }

        }
    }
}
