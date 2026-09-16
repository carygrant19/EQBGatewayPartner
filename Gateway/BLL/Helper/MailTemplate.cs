using Gateway.Data.Models;
namespace Gateway.BLL.Helper
{
    public class MailTemplate
    {
        public static string CreateUser(User user, string encryptionKey)
        {

            string? content = null;

            using (var sr = new StreamReader(string.Format(Directory.GetCurrentDirectory() + "{0}{1}", "/Files/Templates/Mail/", "CreateUser.txt")))
            {
                content = sr.ReadToEnd();
            }

            return string.Format(content, user.FirstName, user.LastName, user.Username, StringManipulation.Decrypt(user.Password!, encryptionKey));
        }
        public static string CreateUserAD(User user, string encryptionKey)
        {

            string? content = null;

            using (var sr = new StreamReader(string.Format(Directory.GetCurrentDirectory() + "{0}{1}", "/Files/Templates/Mail/", "CreateUserAD.txt")))
            {
                content = sr.ReadToEnd();
            }

            return string.Format(content, user.FirstName, user.LastName, user.Username);
        }
        public static string CreateUserPasswordless(User user)
        {

            string? content = null;

            using (var sr = new StreamReader(string.Format(Directory.GetCurrentDirectory() + "{0}{1}", "/Files/Templates/Mail/", "CreateUserPasswordless.txt")))
            {
                content = sr.ReadToEnd();
            }

            return string.Format(content, user.FirstName, user.LastName, user.Username);
        }
        public static string AccountAuthenticationChanged(User user, string message)
        {

            string? content = null;

            using (var sr = new StreamReader(string.Format(Directory.GetCurrentDirectory() + "{0}{1}", "/Files/Templates/Mail/", "AccountAuthenticationTypeChanged.txt")))
            {
                content = sr.ReadToEnd();
            }

            return string.Format(content, user.FirstName, user.LastName, user.Username, message);
        }
        public static string ChangePassword(User user, string encryptionKey)
        {

            string? content = null;
            using (var sr = new StreamReader(string.Format(Directory.GetCurrentDirectory() + "{0}{1}", "/Files/Templates/Mail/", "ChangePassword.txt")))
            {
                content = sr.ReadToEnd();
            }

            return string.Format(content, user.FirstName, user.LastName, user.Username);
        }
        public static string ResetPassword(User user, string encryptionKey)
        {

            string? content = null;
            using (var sr = new StreamReader(string.Format(Directory.GetCurrentDirectory() + "{0}{1}", "/Files/Templates/Mail/", "ResetPassword.txt")))
            {
                content = sr.ReadToEnd();
            }

            return string.Format(content, user.FirstName, user.LastName, user.Username, StringManipulation.Decrypt(user.Password!, encryptionKey));
        }
        public static string UnlockUser(User user)
        {

            string? content = null;

            using (var sr = new StreamReader(string.Format(Directory.GetCurrentDirectory() + "{0}{1}", "/Files/Templates/Mail/", "UnlockUser.txt")))
            {
                content = sr.ReadToEnd();
            }

            return string.Format(content, user.FirstName, user.LastName, user.Username);
        }

        public static string LogException(Exception ex)
        {

            string? content = null;

            using (var sr = new StreamReader(string.Format(Directory.GetCurrentDirectory() + "{0}{1}", "/Files/Templates/Mail/", "LogException.txt")))
            {
                content = sr.ReadToEnd();
            }

            return string.Format(content, ex.Message, ex.Source, ex.StackTrace, DateTime.Now);

        }

    }
}
