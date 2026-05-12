using Newtonsoft.Json;
using Gateway.Data.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Gateway.Helper
{
    public class Utility
    {
        public static DateTime GetSafeDate(object value, DateTime defaultValue)
        {
            if (value == null)
                return defaultValue;

            try
            {
                switch (value)
                {
                    case DateTime dt:
                        return dt;

                    case JsonElement je when je.ValueKind == JsonValueKind.String:
                        if (DateTime.TryParse(je.GetString(), out var parsedFromJson))
                            return parsedFromJson;
                        break;

                    case string s:
                        if (DateTime.TryParse(s, out var parsedFromString))
                            return parsedFromString;
                        break;
                }
            }
            catch
            {
                // ignore and return default
            }

            return defaultValue;
        }

        public static DateTime? TryParseDate(string input)
        {
            if (DateTime.TryParse(input, out DateTime result))
                return result;
            return null;
        }
        public static string EncodeString(string value, string key)
        {
            TripleDESCryptoServiceProvider tripleDESCryptoServiceProvider = new();
            MD5CryptoServiceProvider mD5CryptoServiceProvider = new();
            byte[] array2 = (((SymmetricAlgorithm)(object)tripleDESCryptoServiceProvider).Key = ((HashAlgorithm)(object)mD5CryptoServiceProvider).ComputeHash(Encoding.UTF8.GetBytes(key)));
            ((SymmetricAlgorithm)(object)tripleDESCryptoServiceProvider).Mode = CipherMode.ECB;
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            string text = Convert.ToBase64String(((SymmetricAlgorithm)(object)tripleDESCryptoServiceProvider).CreateEncryptor().TransformFinalBlock(bytes, 0, bytes.Length));
            return text.Replace("=", "-").Replace("+", "_").Replace("/", "*");
        }

        public static string DecodeString(string value, string key)
        {
            TripleDESCryptoServiceProvider tripleDESCryptoServiceProvider = new();
            MD5CryptoServiceProvider mD5CryptoServiceProvider = new();
            byte[] array2 = (((SymmetricAlgorithm)(object)tripleDESCryptoServiceProvider).Key = ((HashAlgorithm)(object)mD5CryptoServiceProvider).ComputeHash(Encoding.UTF8.GetBytes(key)));
            ((SymmetricAlgorithm)(object)tripleDESCryptoServiceProvider).Mode = CipherMode.ECB;
            byte[] array3 = Convert.FromBase64String(value.Replace("-", "=").Replace("_", "+").Replace("*", "/"));
            return Encoding.UTF8.GetString(((SymmetricAlgorithm)(object)tripleDESCryptoServiceProvider).CreateDecryptor().TransformFinalBlock(array3, 0, array3.Length));
        }

        public static string RandomString(int length)
        {
            Random random = new();
            return new string((from s in Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz@", length)
                               select s[random.Next(s.Length)]).ToArray());
        }
        public static int GetInteger(object value)
        {
            return (value != DBNull.Value) ? ((int)value) : 0;
        }

        public static decimal GetDecimal(object value)
        {
            return (value == DBNull.Value) ? 0m : ((decimal)value);
        }

        public static bool GetBool(object value)
        {
            return value != DBNull.Value && (bool)value;
        }

        public static string? GetString(object value)
        {
            return (value == DBNull.Value) ? "" : value.ToString();
        }

        public static DateTime GetDateTime(object value)
        {
            return (value == DBNull.Value) ? default : ((DateTime)value);
        }
        public static string GroupErrors(string json)
        {

            //string jsonResponse = @"{
            //        ""errors"": {
            //            ""BatchDetails[0].SourceBankAccount"": [
            //                ""Field must not be a legacy account.""
            //            ],
            //            ""BatchDetails[0].DestinationBankAccount"": [
            //                ""Field must not be a legacy account.""
            //            ],
            //            ""BatchDetails[1].DestinationBankAccount"": [
            //                ""Field must not be a legacy account.""
            //            ]
            //        }
            //    }";

            // Parse JSON to a dynamic object
            var parsedJson = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);

            // Dictionary to store grouped errors by row
            var groupedErrors = new Dictionary<string, Dictionary<string, List<string>>>();

            // Group errors by index and field
            foreach (var error in parsedJson)
            {
                var keyParts = error.Key.Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);

                if (keyParts.Length >= 2)
                {
                    // Get the index part, e.g., "0" from "BatchDetails[0]"
                    //string rowKey = keyParts[0] + "[" + keyParts[1] + "]";
                    string rowKey = keyParts[1];

                    if (!groupedErrors.ContainsKey(rowKey))
                    {
                        groupedErrors[rowKey] = new Dictionary<string, List<string>>();
                    }

                    // Get the field name and add the errors to the dictionary
                    string fieldKey = keyParts[2].Replace(".", "");
                    groupedErrors[rowKey][fieldKey] = error.Value;
                }
            }

            // Serialize the grouped errors back to JSON
            string groupedJson = JsonConvert.SerializeObject(groupedErrors, Formatting.Indented);

            return groupedJson;

        }

        public static List<AllMonth> GetAllMonths()
        {
            return new List<AllMonth>
        {
            new() { MonthName = "January", MonthNo = 1 },
            new() { MonthName = "February", MonthNo = 2 },
            new() { MonthName = "March", MonthNo = 3 },
            new() { MonthName = "April", MonthNo = 4 },
            new() { MonthName = "May", MonthNo = 5 },
            new() { MonthName = "June", MonthNo = 6 },
            new() { MonthName = "July", MonthNo = 7 },
            new() { MonthName = "August", MonthNo = 8 },
            new() { MonthName = "September", MonthNo = 9 },
            new() { MonthName = "October", MonthNo = 10 },
            new() { MonthName = "November", MonthNo = 11 },
            new() { MonthName = "December", MonthNo = 12 }
        };
        }
    }
}