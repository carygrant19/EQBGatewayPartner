using Models = Gateway.Data.Models;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace Gateway.BLL.Services
{ 
    public interface ICertificateValidatorService
    {
        bool Validate(X509Certificate2? clientCertificate);
    }
}