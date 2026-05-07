using System.ComponentModel.DataAnnotations;

namespace Temenos.API.DTOs.Request
{
    public class Authenticate
    {
        [Required(ErrorMessage = "Usename is required")]
        public string Username { get; set; } = string.Empty!;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty!;
    }
}
