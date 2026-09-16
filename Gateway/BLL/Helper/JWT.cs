using Gateway.Data.Models; 
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Gateway.BLL.Helper;

public static class JwtHelper
{
    public static string GenerateToken(string clientCode, AuthProvider provider)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(provider.SecretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, clientCode),
                new Claim("client_code", clientCode)
            }),
            Expires = DateTime.UtcNow.AddMinutes(provider.TokenLifetimeMinutes),
            Issuer = provider.Issuer,
            Audience = provider.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public static bool ValidateToken(string token, AuthProvider provider)
    {
        if (string.IsNullOrWhiteSpace(token) || provider == null || !provider.IsActive)
            return false;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(provider.SecretKey);

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = !string.IsNullOrEmpty(provider.Issuer),
                ValidIssuer = provider.Issuer,
                ValidateAudience = !string.IsNullOrEmpty(provider.Audience),
                ValidAudience = provider.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out SecurityToken validatedToken);

            return validatedToken != null;
        }
        catch
        {
            return false;
        }
    }
}