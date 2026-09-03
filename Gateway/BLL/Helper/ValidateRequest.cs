using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Helper
{
    public static class ValidateRequest
    {
        public static async Task<string> SignatureAsync(HttpContext context, Response.Client client, string? apiSecret = null)
        {
            var request = context.Request;

            if (!request.Headers.TryGetValue("X-Signature", out var signatureHeader))
            {
                return "No signature.";
            }

            var signature = signatureHeader.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(signature))
            {
                return "No signature.";
            }

            string signedText;
            if (request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) ||
                request.Method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                // Key as Signature for GET/DELETE
                signedText = client.ApiKey;
            }
            else
            {
                // Payload as Signature for POST/PUT/PATCH
                signedText = await ReadBodyAsync(request);
            }

            string secretToUse = !string.IsNullOrEmpty(apiSecret) ? apiSecret : client.ApiKey;

            if (IsValidSignature(signedText, signature, secretToUse))
            {
                return "Valid";
            }

            return "Invalid signature.";
        }

        #region Helper Methods

        public static string Generate(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            byte[] hash = string.IsNullOrEmpty(data)
                ? hmac.ComputeHash(Encoding.UTF8.GetBytes(key))
                : hmac.ComputeHash(Encoding.UTF8.GetBytes(data));

            return Convert.ToBase64String(hash);
        }

        private static async Task<string> ReadBodyAsync(HttpRequest request)
        {
            if (!request.Body.CanSeek)
            {
                request.EnableBuffering();
            }

            request.Body.Position = 0;

            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            request.Body.Position = 0;

            return body;
        }

        private static bool IsValidSignature(string data, string signature, string secret)
        {
            var computedSignature = Generate(data, secret);
            return string.Equals(computedSignature, signature, StringComparison.Ordinal);
        }

        #endregion

        #region Certificate Validation

        public static bool ValidateCertificate(X509Certificate2? clientCertificate, X509Certificate2? systemCertificate)
        {
            if (clientCertificate == null || systemCertificate == null)
                return false;

            return string.Equals(clientCertificate.Thumbprint, systemCertificate.Thumbprint, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}