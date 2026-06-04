using Notification.API.Services.IService;
using System.Net;
using System.Net.Mail;
using DTO = Notification.API.DTOs;
using Model = Notification.API.Models;

namespace Notification.API.Services
{
    public class SMTPService(ILogger<SMTPService> logger) : ISMTPService
    {
        private readonly ILogger<SMTPService> _logger = logger;

        public async Task<string> Send(
           Model.SMTPConfig config,
           DTO.Request.Mail request,
           string mailType
        )
        {
            string result = "";

            try
            {
                var account = config.Accounts.FirstOrDefault(a => a.Type.Equals(mailType, StringComparison.CurrentCultureIgnoreCase));

                using var smtpClient = new SmtpClient(config.Server, config.Port)
                {
                    UseDefaultCredentials = config.UseDefaultCredentials,
                    EnableSsl = config.EnableSSL,
                    Credentials = new NetworkCredential(account!.Username, account.Password)
                };

                using var mail = new MailMessage
                {
                    From = new MailAddress(account.Username!),
                    Subject = request.Subject,
                    SubjectEncoding = System.Text.Encoding.UTF8,
                    IsBodyHtml = request.IsHTML,
                    BodyEncoding = System.Text.Encoding.UTF8,
                    Body = request.Body
                };

                if (!string.IsNullOrEmpty(request.To))
                {
                    foreach (var addr in request.To.Split(';', StringSplitOptions.RemoveEmptyEntries))
                        mail.To.Add(addr);
                }

                if (!string.IsNullOrEmpty(request.Cc))
                {
                    foreach (var addr in request.Cc.Split(';', StringSplitOptions.RemoveEmptyEntries))
                        mail.CC.Add(addr);
                }

                if (!string.IsNullOrEmpty(request.Bcc))
                {
                    foreach (var addr in request.Bcc.Split(';', StringSplitOptions.RemoveEmptyEntries))
                        mail.Bcc.Add(addr);
                }

                if (request.Attachments != null && request.Attachments.Count > 0)
                {
                    foreach (var att in request.Attachments)
                    {
                        var stream = new MemoryStream(att.File);
                        mail.Attachments.Add(new Attachment(stream, att.Name));
                    }
                }

                _logger.LogInformation("Sending email | Payload: {@request}", System.Text.Json.JsonSerializer.Serialize(request));

                await smtpClient.SendMailAsync(mail);

                _logger.LogInformation("Mail sent");

                result = "success";

            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "Sending Failed");
                throw new ApplicationException("SmtpException has occurred: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sending Failed");
                throw new ApplicationException("Exception has occurred: " + ex.Message);
            }

            return result;

        }
    }
}
