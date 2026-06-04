using Common.Services.IService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Request = Common.DTOs.Request;
using Response = Common.DTOs.Response;

namespace Common.Services
{
    public class AuthenticateService(IConfiguration configuration, ILogger<AuthenticateService> logger) : IAuthenticateService
    {
        private readonly ILogger<AuthenticateService> _logger = logger;

        public async Task<Response.Authenticate> Authenticate(Request.Authenticate model)
        {
            Response.Authenticate response = new();

            _logger.LogInformation("Authenticating | User: {User}", model.Username);

            try
            {
                if (model.Username == configuration["Credential:Username"] && model.Password == configuration["Credential:Password"])
                {
                    response = new Response.Authenticate
                    {
                        Status = "SUCCESS",
                        Message = "Authenticated",
                        Token = GenerateToken(model)
                    };
                }
                else
                {
                    response.Status = "FAILED";
                    response.Message = "Invalid username or password.";
                }

                _logger.LogInformation("Authenticating {Status} | User: {User}", response.Status, model.Username);

            }
            catch (Exception ex)
            {
                response = new()
                {
                    Status = "ERROR",
                    Message = ex.Message
                };

                _logger.LogError(ex, "FundTransfer Unexpected Error | User: {User} | Response: {@Response}", model.Username, response);
            }

            return await Task.FromResult(response);
        }

        #region private 
        private string GenerateToken(Request.Authenticate model)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Name, model.Username!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                 issuer: configuration["JWT:Issuer"]!,
                 audience: configuration["JWT:Audience"]!,
                 claims: claims,
                 expires: DateTime.Now.AddDays(int.Parse(configuration["JWT:Duration"]!)), //modify
                 signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
             );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        //method 

        #endregion


    }
}
