using System.ComponentModel.DataAnnotations;

namespace Gateway.BLL.DTO.Request
{
    public class AuthProvider : Base
    {
        public string Id { get; set; }
         
        public string Code { get; set; } = string.Empty;
         
        public string Name { get; set; } = string.Empty; 
        public string? Issuer { get; set; }
 
        public string Audience { get; set; } = string.Empty;
         
        public string SecretKey { get; set; } = string.Empty;

        public int TokenLifetimeMinutes { get; set; } = 60;

        public bool IsActive { get; set; } = true;
    }
}