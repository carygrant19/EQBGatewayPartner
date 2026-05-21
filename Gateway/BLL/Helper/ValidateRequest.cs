using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Response = Gateway.BLL.DTO.Response;
namespace Gateway.BLL.Helper
{
    public static class ValidateRequest
    {
        public static string Signature(HttpContext context, Response.ApiClient client)
        {
            var request = context.Request;

            if (!request.Headers.TryGetValue("X-Signature", out var signatureHeader))
            {
                return "No signature.";
            }

            var signature = signatureHeader.FirstOrDefault();
            //var body = ReadBodyAsync(request).Result;
            var signedText = "";
            if (context.Request.Method.Equals("get", StringComparison.OrdinalIgnoreCase) || context.Request.Method.Equals("delete", StringComparison.OrdinalIgnoreCase))
            {
                //Key as Signature
                signedText = client.ApiKey;
            }
            else 
            {
                //Payload as Signature
                signedText = ReadBodyAsync(request).Result;
                
            }

            

            if (IsValidSignature(signedText, signature!, client.ApiSecret))
            {
                return "Valid";
            }
            else
            {
                return "Invalid signature.";
            }
        }

        #region methods
        public static string Generate(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            byte[] hash;

            if (string.IsNullOrEmpty(data))
            {
                hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(key));
            }
            else
            {
                hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            }

            return Convert.ToBase64String(hash);
        }
        private static async Task<string> ReadBodyAsync(HttpRequest request)
        {
            if (!request.Body.CanSeek)
            {
                request.EnableBuffering();
            }

            request.Body.Position = 0;

            var reader = new StreamReader(request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync().ConfigureAwait(false);

            request.Body.Position = 0;

            return body;
        }
        private static bool IsValidSignature(string data, string signature, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));

            byte[] computedHash;

            if (string.IsNullOrEmpty(data))
            {
                computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(secret));
            }
            else
            {
                computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            }

            var computedSignature = Convert.ToBase64String(computedHash);
            return computedSignature == signature;
        }

        #endregion


        #region Certificate

        public static bool ValidateCertificate(X509Certificate2 clientCertificate, X509Certificate2 systemCertificate)
        { 
            if (clientCertificate.Thumbprint == systemCertificate.Thumbprint)
                return true;

            return false;
        }

        #endregion


    }
}
