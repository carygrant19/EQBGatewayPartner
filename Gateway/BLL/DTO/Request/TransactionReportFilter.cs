using System;

namespace Gateway.BLL.DTO.Request
{
    public class TransactionReportFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchTerm { get; set; }
        public string? RouteId { get; set; }
        public string StatusCategory { get; set; } = "ALL"; // ALL, 2XX, 4XX, 5XX
        public int PageNum { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }
}