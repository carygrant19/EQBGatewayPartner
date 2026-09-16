using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models;

[Table("Master_AuthProviders")]
public class AuthProvider
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Audience { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string SecretKey { get; set; } = string.Empty;

    public int TokenLifetimeMinutes { get; set; } = 60;

    public bool IsActive { get; set; } = true;
}