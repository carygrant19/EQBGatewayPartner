using Models = Gateway.Data.Models;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace Gateway.BLL.Services
{
    public class CertificateValidatorService : ICertificateValidatorService, IDisposable
    {
        private readonly X509Certificate2 _trustedCertificate;

        public CertificateValidatorService(IOptions<Models.CertificateOptions> options)
        {
            // Load the certificate once into memory when the service starts
            _trustedCertificate = new X509Certificate2(options.Value.Path, options.Value.Password);
        }

        public bool Validate(X509Certificate2? clientCertificate)
        {
            if (clientCertificate == null)
                return false;

            // Strict Thumbprint match (Case-Insensitive for safety)
            return string.Equals(
                _trustedCertificate.Thumbprint,
                clientCertificate.Thumbprint,
                StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            // Explicitly release the certificate handle
            _trustedCertificate?.Dispose();
            GC.SuppressFinalize(this);
        }
    } 
    
}