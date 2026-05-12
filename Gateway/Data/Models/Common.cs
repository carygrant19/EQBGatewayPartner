namespace Gateway.Data.Models
{
    public class Filter
    {
        public string? Property { get; set; }
        public string? Operator { get; set; }
        public object? Value { get; set; }
        public object? Value2 { get; set; } // For BETWEEN operator
        public IEnumerable<object>? Values { get; set; } // For IN operator 
        public Boolean IsDate { get; set; } = false;
    }

    public static class Operators
    {
        public new const string Equals = "EQUALS";
        public const string GreaterThan = "GREATERTHAN";
        public const string LessThan = "LESSTHAN";
        public const string GreaterThanOrEqual = "GREATERTHANOREQUAL";
        public const string LessThanOrEqual = "LESSTHANOREQUAL";
        public const string Contains = "CONTAINS";
        public const string StartsWith = "STARTSWITH";
        public const string EndsWith = "ENDSWITH";
        public const string Between = "BETWEEN";
        public const string In = "IN";
        public const string IsNull = "ISNULL";
        public const string IsNotNull = "ISNOTEQUAL";
        public const string NotEquals = "NOTEQUALS";
    }

    public class SweetAlertMessage
    {
        public string? Title { get; set; }
        public string? Message { get; set; }
        public string? MessageType { get; set; }
    }
    public class AllMonth
    {
        public string MonthName { get; set; } = string.Empty;
        public int MonthNo { get; set; }
    }
    public class AppConfig
    {
        public string? AppCode { get; set; }
        public string? AppName { get; set; }
        public string? AppUrl { get; set; } 
        public string? Token { get; set; }
        public List<string>? HeadOffice { get; set; }
    }

    public class SystemParameters
    {
        public int? PasswordMaxTry { get; set; } = 3;
        public double? SessionTimeOutMinutes { get; set; } = 90;
        public int? PasswordCycleMaxCount { get; set; } = 4;
        public int? AccountDormancyDays { get; set; } = 90;
        public int? NotifyPasswordExpiryDays { get; set; } = 3;
    }
    public class CertificateOptions
    {
        public string? Path { get; set; }
        public string? Password { get; set; }
    }
    public class Error
    {
        public string? Code { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
    }
    public class MailSettings
    {
        public bool Enable { get; set; } = false;
        public string? Server { get; set; }
        public int Port { get; set; }
        public bool EnableSSL { get; set; }
        public string? Username { get; set; }
        public string? AccountPassword { get; set; }
        public bool IsHTML { get; set; }
        public string? From { get; set; }
        public string? Subject { get; set; }
        public bool UseDefaultCredentials { get; set; }
        public string? AttachmentPassword { get; set; }
    }
    public class MailAttachments
    {
        public string? Filename { get; set; }
        public byte[]? FileBytes { get; set; }
    }

}
