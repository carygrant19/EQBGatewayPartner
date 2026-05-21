using Gateway.BLL.Helper;
using Model = Gateway.Data.Models;
namespace Gateway.BLL.Services.IServices
{
    public interface ILogService
    {
        void LogActivity(Model.ActivityLog model);
        void LogAudit(Model.AuditLog model);
        void LogException(Exception ex, string moduleName);
        void LogTransaction(Model.TransactionLog model, string type);
        void LogHttp(Model.HttpLog model, string type);
    }

    public class LogService(EFDbContext efDbContext) : ILogService
    {
        private readonly EFDbContext _efDbContext = efDbContext;

        public void LogActivity(Model.ActivityLog model)
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
        public void LogAudit(Model.AuditLog model)
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
                Model.ExceptionLog model = new()
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
        public void LogTransaction(Model.TransactionLog model, string type)
        {
            try
            {
                if (type == "REQUEST")
                {
                    _efDbContext.TransactionLog!.Add(model);
                    _efDbContext.SaveChanges();

                }
                else
                {
                    var data = _efDbContext.TransactionLog!.FirstOrDefault(l => l.TransactionId.Trim() == model.TransactionId.Trim())!;

                    data.Response = model.Response;
                    data.ResponseDate = model.ResponseDate;
                    data.Status = model.Status;
                    data.CheckSum = model.CheckSum;

                    _efDbContext.TransactionLog!.Update(data);
                    _efDbContext.SaveChanges();

                }
            }
            catch (Exception ex)
            {
                LogException(ex, "LogTransaction");
            }
        }

        public void LogHttp(Model.HttpLog model, string type)
        {
            try
            {
                if (type == "REQUEST")
                {
                    _efDbContext.HttpLog!.Add(model);
                    _efDbContext.SaveChanges();

                }
                else
                {
                    var data = _efDbContext.HttpLog!.FirstOrDefault(l => l.TraceId!.Trim() == model.TraceId!.Trim())!;
                    data.ResponseData = model.ResponseData;
                    data.ResponseDate = model.ResponseDate;
                    data.ResponseCode = model.ResponseCode;

                    _efDbContext.HttpLog!.Update(data);
                    _efDbContext.SaveChanges();

                }
            }
            catch (Exception exx)
            {

            }
        }

    }
}
