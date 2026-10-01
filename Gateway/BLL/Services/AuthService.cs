using AutoMapper;
using Gateway.BLL.Helper;
using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Gateway.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model = Gateway.Data.Models;

namespace Gateway.BLL.Services
{
    public class AuthService : IAuthService
    { 
        private readonly ILogService _logService;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly string _moduleName = "AuthenticationService";

        public AuthService(ILogService logService, IMapper mapper, IConfiguration configuration)
        { 
            _logService = logService;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<(bool IsSuccess, string JsonResponse, int StatusCode)> ProcessInternalAuthAsync(HttpContext context, Model.Route endpoint)
        {
            try
            {
                var request = context.Request;

                // 1. Extract Inbound Credentials directly from HTTP Headers / Basic Auth
                var (apiKey, apiSecret) = ExtractCredentialsFromHeaders(request);

                // 2. Strict Validation: Requires both API Key and Secret
                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
                {
                    return (false, JsonConvert.SerializeObject(new
                    {
                        error = "Invalid Request",
                        message = "Missing client headers. Please provide X-Api-Key and X-Api-Secret"
                    }), 400);
                }

                // 3. IN-MEMORY VALIDATION: Hanapin ang Client at Credentials sa RAM (endpoint.ClientRouteAccess)
                if (endpoint.ClientRouteAccess == null || !endpoint.ClientRouteAccess.Any())
                {
                    return (false, JsonConvert.SerializeObject(new
                    {
                        error = "Unauthorized Client",
                        message = "No client access configured for this route."
                    }), 403);
                }

                Model.ClientCredential? matchedCredential = null;
                Model.Client? matchedClient = null;

                foreach (var routeAccess in endpoint.ClientRouteAccess)
                {
                    // Filter: Allowed route access at hindi pa expired
                    if (!routeAccess.IsAllowed) continue;
                    if (routeAccess.ExpiresAt.HasValue && routeAccess.ExpiresAt.Value <= DateTime.UtcNow) continue;

                    var client = routeAccess.Client;
                    if (client == null || client.Deleted || !string.Equals(client.Status, "Active", StringComparison.OrdinalIgnoreCase)) continue;

                    // Match API Key laban sa Credentials collection sa RAM
                    var cred = client.Credentials?.FirstOrDefault(c => c.IsActive && c.ApiKey == apiKey);
                    if (cred != null)
                    {
                        matchedCredential = cred;
                        matchedClient = client;
                        break;
                    }
                }

                if (matchedCredential == null || matchedClient == null)
                {
                    return (false, JsonConvert.SerializeObject(new
                    {
                        error = "Invalid Client",
                        message = "Client credentials not found or account is not authorized for this route."
                    }), 401);
                }

                // 4. Verify Secret Hash (In-Memory)
                bool isValidSecret = VerifySecret(apiSecret, matchedCredential.ApiSecret);
                if (!isValidSecret)
                {
                    return (false, JsonConvert.SerializeObject(new
                    {
                        error = "Invalid API Key",
                        message = "Invalid API Secret."
                    }), 401);
                }

                // 5. IN-MEMORY AUTH PROVIDER RESOLUTION
                Model.AuthProvider? provider = endpoint.AuthProvider;

                // Fallback kung walang naka-bind na AuthProvider ID sa Route
                provider ??= new Model.AuthProvider
                {
                    Code = "GATEWAY",
                    Name = "Gateway",
                    Issuer = _configuration["JWT:Issuer"] ?? "Gateway",
                    Audience = _configuration["JWT:Audience"] ?? "Gateway",
                    SecretKey = _configuration["JWT:Secret"] ?? "",
                    TokenLifetimeMinutes = 60,
                    IsActive = true
                };

                // 6. Save Matched Client to HttpContext for Audit Logs
                context.Items["MatchedClient"] = matchedClient;

                // 7. Generate Signed JWT Token (Pure In-Memory)
                int lifetimeMinutes = provider.TokenLifetimeMinutes > 0 ? provider.TokenLifetimeMinutes : 60;
                int lifetimeSeconds = lifetimeMinutes * 60;

                string jwtToken = JwtHelper.GenerateToken(matchedClient.Code, provider, lifetimeSeconds);

                var successPayload = new
                {
                    access_token = jwtToken,
                    token_type = "Bearer",
                    expires_in = lifetimeSeconds,
                    client_code = matchedClient.Code,
                    issued_at = DateTime.UtcNow
                };

                return await Task.FromResult((true, JsonConvert.SerializeObject(successPayload), 200));
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, _moduleName);
                return (false, JsonConvert.SerializeObject(new
                {
                    error = "Server Error",
                    message = "An error occurred during authentication processing."
                }), 500);
            }
        }

        private static (string? ApiKey, string? ApiSecret) ExtractCredentialsFromHeaders(HttpRequest request)
        {
            string? apiKey = GetHeaderValue(request, "X-Api-Key", "X-Client-Id", "client_id");
            string? apiSecret = GetHeaderValue(request, "X-Api-Secret", "X-Client-Secret", "client_secret");

            if ((string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret)) &&
                request.Headers.TryGetValue("Authorization", out var authHeader) &&
                authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == false &&
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