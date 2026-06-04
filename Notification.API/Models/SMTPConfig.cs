
namespace Notification.API.Models
{
    public class SMTPConfig
    {
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; } = 0;
        public bool UseDefaultCredentials { get; set; } = false;
        public bool EnableSSL { get; set; } = false;
        public List<SMTPAccount> Accounts { get; set; } = [];

    }

    public class SMTPAccount
    {
        public string Type { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
