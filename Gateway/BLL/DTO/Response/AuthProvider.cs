namespace Gateway.BLL.DTO.Response
{
    public class VAuthProvider : ListBase
    {
        public List<FAuthProvider> Data { get; set; } = [];
    }

    public class FAuthProvider : AuthProvider
    {
        public bool Deleted { get; set; }
    }
    public class AuthProvider : Base
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string? Issuer { get; set; }

        public string Audience { get; set; } = string.Empty;

        public string SecretKey { get; set; } = string.Empty;

        public int TokenLifetimeMinutes { get; set; } = 60;

        public bool IsActive { get; set; } = true;
    }
}