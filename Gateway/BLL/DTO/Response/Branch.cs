using Gateway.Data.Models;

namespace Gateway.BLL.DTO.Response
{
    public class VBranch : ListBase
    {
        public List<FBranch> Data { get; set; } = [];
    }
    public class FBranch : Branch
    {
        public bool Deleted { get; set; }
    }
    public class Branch
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string BankingHour { get; set; } = string.Empty;
        public string Officer { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ContactNo { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public AccountMapping? AccountMappings { get; set; }

    }
}
