using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Temenos.API.Sevices.IService;
using Request = Temenos.API.DTOs.Request;
using Response = Temenos.API.DTOs.Response;

namespace Temenos.API.Sevices
{
    public class AuthenticateService(IConfiguration configuration, ILogger<TransactionService> logger) : IAuthenticateService
    {
        private readonly ILogger<TransactionService> _logger = logger;
        private readonly IConfiguration _configuration = configuration;
        public async Task<Response.Authenticate> Authenticate(Request.VendorHeaderRequest headers, Request.Authenticate model)
        {
            Response.Authenticate response = new();

            // 1. Extract values mula sa 4 na vendor headers
            var vendorUsername = headers?.VendorUsername;
            var vendorPassword = headers?.VendorPassword;
            var vendorApiKey = headers?.VendorApiKey;
            var vendorApiSecret = headers?.VendorApiSecret;

            _logger.LogInformation("Authenticating Vendor | VendorUser: {VendorUser} | VendorKey: {VendorKey} | ClientUser: {User}",
                vendorUsername, vendorApiKey, model.Username);

            try
            {
                // 2. Validation kung may kulang na vendor header
                if (string.IsNullOrEmpty(vendorUsername) ||
                    string.IsNullOrEmpty(vendorPassword) ||
                    string.IsNullOrEmpty(vendorApiKey) ||
                    string.IsNullOrEmpty(vendorApiSecret))
                {
                    response.Status = "FAILED";
                    response.Message = "Missing required X-Vendor headers.";
                    _logger.LogWarning("Authentication FAILED | Missing vendor headers for user: {User}", model.Username);
                    return response;
                }

                // 3. I-validate ang Vendor Credentials at Client User Credentials
                bool isVendorValid = vendorUsername == _configuration["VendorCredential:Username"] &&
                                     vendorPassword == _configuration["VendorCredential:Password"] &&
                                     vendorApiKey == _configuration["VendorCredential:ApiKey"] &&
                                     vendorApiSecret == _configuration["VendorCredential:ApiSecret"];
                 

                if (isVendorValid)
                {
                    response = new Response.Authenticate
                    {
                        Status = "SUCCESS",
                        Message = "Authenticated successfully.",
                        Token = GenerateToken(model)
                    };
                }
                else
                {
                    response.Status = "FAILED";
                    response.Message = "Invalid credentials.";
                }

                _logger.LogInformation("Authenticating {Status} | VendorUser: {VendorUser} | ClientUser: {User}",
                    response.Status, vendorUsername, model.Username);
            }
            catch (Exception ex)
            {
                response = new Response.Authenticate
                {
                    Status = "ERROR",
                    Message = ex.Message
                };

                _logger.LogError(ex, "Authentication Unexpected Error | VendorUser: {VendorUser} | ClientUser: {User} | Response: {@Response}",
                    vendorUsername, model.Username, response);
            }

            return response;
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
                 expires: DateTime.Now.AddDays(365),
                 signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
             );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        #endregion

    }
}
