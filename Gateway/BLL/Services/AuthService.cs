using AutoMapper;
using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Gateway.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;
using Model = Gateway.Data.Models;

namespace Gateway.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly EFDbContext _efDbContext;
        private readonly ILogService _logService;
        private readonly IMapper _mapper;
        private readonly string _moduleName = "AuthenticationService";

        public AuthService(EFDbContext efDbContext, ILogService logService, IMapper mapper)
        {
            _efDbContext = efDbContext;
            _logService = logService;
            _mapper = mapper;
        }

        public async Task<(bool IsSuccess, string JsonResponse, int StatusCode)> ProcessInternalAuthAsync(HttpContext context, Model.Route endpoint)
        {
            try
            {
                var request = context.Request;

                // 1. Extract Inbound Credentials directly from HTTP Headers
                var (apiKey, apiSecret) = ExtractCredentialsFromHeaders(request);

                // 2. Strict Validation: Requires both API Key and Secret
                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
                {
                    return (false, JsonConvert.SerializeObject(new
                    {
                        error = "Invalid Request",
                        message = "Missing client headers. Please provide X-Api-Key and X-Api-Secret (or X-Client-Id and X-Client-Secret)."
                    }), 400);
                }

                // 3. Query Map_Client_Credential joined with Master_Client
                var clientCredential = await _efDbContext.Set<Model.ClientCredential>()
                    .Include(cc => cc.Client)
                    .FirstOrDefaultAsync(cc => cc.IsActive
                        && cc.ApiKey == apiKey
                        && cc.Client != null
                        && !cc.Client.Deleted
                        && cc.Client.Status == "Active");

                if (clientCredential == null || clientCredential.Client == null)
                {
                    return (false, JsonConvert.SerializeObject(new { error = "Invalid Client", message = "Client credentials not found or account is inactive." }), 401);
                }

                // 4. Verify Secret Hash
                bool isValidSecret = VerifySecret(apiSecret, clientCredential.ApiSecretHash);
                if (!isValidSecret)
                {
                    return (false, JsonConvert.SerializeObject(new { error = "Invalid API Key", message = "Invalid API Secret." }), 401);
                }

                // 5. Validate Client Route Access (Map_Client_Route_Access)
                var routeAccess = await _efDbContext.Set<Model.ClientRouteAccess>()
                    .FirstOrDefaultAsync(ra => ra.ClientId == clientCredential.ClientId
                        && ra.RouteId == endpoint.Id
                        && ra.IsAllowed
                        && (ra.ExpiresAt == null || ra.ExpiresAt > DateTime.UtcNow));

                if (routeAccess == null)
                {
                    return (false, JsonConvert.SerializeObject(new { error = "Unauthorized Client", message = "Client is not authorized to access this route." }), 403);
                }

                // 6. Fetch Master_AuthProviders configuration
                Model.AuthProvider? provider = null;
                if (endpoint.AuthProviderId.HasValue && endpoint.AuthProviderId.Value > 0)
                {
                    provider = await _efDbContext.Set<Model.AuthProvider>()
                        .FirstOrDefaultAsync(p => p.Id == endpoint.AuthProviderId.Value && p.IsActive);
                }

                provider ??= await _efDbContext.Set<Model.AuthProvider>().FirstOrDefaultAsync(p => p.IsActive);

                if (provider == null)
                {
                    return (false, JsonConvert.SerializeObject(new { error = "Server Error", message = "No active AuthProvider scheme configured." }), 500);
                }

                // 7. Save Matched Client to HttpContext for Audit Logs
                context.Items["MatchedClient"] = clientCredential.Client;

                // 8. Generate Signed JWT Token
                int lifetimeMinutes = provider.TokenLifetimeMinutes > 0 ? provider.TokenLifetimeMinutes : 60;
                int lifetimeSeconds = lifetimeMinutes * 60;

                string jwtToken = JwtHelper.GenerateToken(clientCredential.Client.Code, provider, lifetimeSeconds);

                var successPayload = new
                {
                    access_token = jwtToken,
                    token_type = "Bearer",
                    expires_in = lifetimeSeconds,
                    client_code = clientCredential.Client.Code,
                    issued_at = DateTime.UtcNow
                };

                return (true, JsonConvert.SerializeObject(successPayload), 200);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                return (false, JsonConvert.SerializeObject(new { error = "Server Error", message = "An error occurred during authentication processing." }), 500);
            }
        }

        private static (string? ApiKey, string? ApiSecret) ExtractCredentialsFromHeaders(HttpRequest request)
        {
            string? apiKey = GetHeaderValue(request, "X-Api-Key", "X-Client-Id", "client_id");
            string? apiSecret = GetHeaderValue(request, "X-Api-Secret", "X-Client-Secret", "client_secret");

            // Option: Support standard Authorization: Basic Base64(ApiKey:ApiSecret)
            if ((string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret)) &&
                request.Headers.TryGetValue("Authorization", out var authHeader) &&
                authHeader.ToString().StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var encoded = authHeader.ToString()["Basic ".Length..].Trim();
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                    var parts = decoded.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        apiKey ??= parts[0];
                        apiSecret ??= parts[1];
                    }
                }
                catch { /* Ignore bad base64 */ }
            }

            return (apiKey, apiSecret);
        }

        private static string? GetHeaderValue(HttpRequest request, params string[] possibleKeys)
        {
            foreach (var key in possibleKeys)
            {
                if (request.Headers.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                {
                    return val.ToString().Trim();
                }
            }
            return null;
        }

        private static bool VerifySecret(string inputSecret, string dbSecretHash)
        {
            if (inputSecret == dbSecretHash)
                return true;

            try
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var inputBytes = Encoding.UTF8.GetBytes(inputSecret);
                var hashBytes = sha256.ComputeHash(inputBytes);
                var computedHash = Convert.ToHexString(hashBytes);

                return string.Equals(computedHash, dbSecretHash, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}