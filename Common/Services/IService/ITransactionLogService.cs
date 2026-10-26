namespace Common.Services.IService
{
    public interface ITransactionLogService
    {
        Task<long> LogRequestAsync(string? transactionId, string urlEndpoint, string requestPayload, DateTime requestDate);
        Task LogResponseAsync(long logId, string responsePayload, DateTime responseDate, string status);
        Task LogExceptionAsync(string moduleName, Exception ex);
        string GetCurrentTraceId();
    }
}