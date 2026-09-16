using Gateway.Data.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Gateway.BLL.Services.IServices
{
    public interface IMailerService
    {
        Task<string> Send(
            string mailTo,
            string subject,
            string body,
            bool isBodyHTML,
            string? mailCc = null,
            string? mailBcc = null,
            List<(byte[] File, string Name)>? attachments = null
        );
    }
    public class MailerService : IMailerService
    {
        private readonly MailSettings _config;

        public MailerService(MailSettings config)
        {
            _config = config;
        }

        public async Task<string> Send(
            string mailTo,
            string subject,
            string body,
            bool isBodyHTML,
            string? mailCc = null,
            string? mailBcc = null,
            List<(byte[] File, string Name)>? attachments = null
        )
        {
            try
            { 
                if (!_config.Enable)
                    return "";

                using var smtpClient = new SmtpClient(_config.Server, _config.Port)
                {
                    UseDefaultCredentials = _config.UseDefaultCredentials,
                    EnableSsl = _config.EnableSSL,
                    Credentials = new NetworkCredential(_config.Username, _config.AccountPassword)
                };

                using var mail = new MailMessage
                {
                    From = new MailAddress(_config.From!),
                    Subject = subject,
                    SubjectEncoding = Encoding.UTF8,
                    IsBodyHtml = isBodyHTML,
                    BodyEncoding = Encoding.UTF8,
                    Body = body
                };

                ParseAddresses(mail.To, mailTo);
                ParseAddresses(mail.CC, mailCc);
                ParseAddresses(mail.Bcc, mailBcc);

                if (attachments?.Count > 0)
                {
                    foreach (var att in attachments)
                    {
                        var stream = new MemoryStream(att.File);
                        mail.Attachments.Add(new Attachment(stream, att.Name));
                    }
                }

                await smtpClient.SendMailAsync(mail);
                return "success";
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Mail failure: " + ex.Message);
            }
        }

        private void ParseAddresses(MailAddressCollection collection, string? addresses)
        {
            if (string.IsNullOrEmpty(addresses)) return;
            foreach (var addr in addresses.Split(';', StringSplitOptions.RemoveEmptyEntries))
                collection.Add(addr);
        }
    }
}


