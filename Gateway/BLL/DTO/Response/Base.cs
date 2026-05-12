namespace Gateway.BLL.DTO.Response
{
    public class Result
    {
        public string? Status { get; set; } = string.Empty;
        public string? Message { get; set; } = string.Empty;
        public List<string>? Errors { get; set; } = new();
    }

    public class Base
    {
        public string? CreatedBy { get; set; } = default!;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public string? UpdatedBy { get; set; } = default!;
        public DateTime? UpdatedDate { get; set; }
    }

    public class ListBase
    {
        public int TotalPage { get; set; } = 0;
        public int CurrentPage { get; set; } = 0;
        public int TotalRecord { get; set; } = 0;
    }
}
