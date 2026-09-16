using Gateway.BLL.Services.IService;
using Gateway.BLL.Services.IServices;
using Gateway.BLL.Helper; 
using Microsoft.Extensions.Configuration;
using System.DirectoryServices;
using Model = Gateway.Data.Models;
using Request = Gateway.BLL.DTO.Request;
using Response = Gateway.BLL.DTO.Response;

namespace Gateway.BLL.Services
{
    public class LDAPService : ILDAPService
    {
        readonly IConfiguration _configuration;
        readonly IUserService _userService;
        readonly IRepository<Model.ActiveUser> _activeUserRepository;
        readonly IRepository<Model.User> _userRepository;
        readonly EFDbContext _efDbContext;
        readonly Model.SystemParameters _systemParameters;
        readonly string _activeDirectoryDomain;
        public LDAPService(EFDbContext efDbContext, IConfiguration configuration, IUserService userService, IRepository<Model.User> userRepository, IRepository<Model.ActiveUser> activeUserRepository)
        {
            _configuration = configuration;
            _userService = userService;
            _activeUserRepository = activeUserRepository;
            _efDbContext = efDbContext;
            _systemParameters = configuration.GetSection("SystemParameters").Get<Model.SystemParameters>()!;
            _userRepository = userRepository;
            _activeDirectoryDomain = configuration["ActiveDirectoryConfig:Domain"]!; 
        }
 
        private string GetLdapValue(SearchResult result, string propertyName)
        {
            try
            {
                if (result.Properties.Contains(propertyName) &&
                    result.Properties[propertyName].Count > 0 &&
                    result.Properties[propertyName][0] != null)
                {
                    return result.Properties[propertyName][0].ToString()!;
                }
            }
            catch { }

            return string.Empty;  
        }
        

        public async Task<Response.User> Login(Request.User user)
        {
            Response.User userInfo = new();

            string domainName = _activeDirectoryDomain;
            string ldapPath = $"LDAP://{domainName}";

            try
            {
                using (DirectoryEntry entry = new DirectoryEntry(ldapPath, $"{domainName}\\{user.Username}", user.Password))
                {
                    object nativeObject = entry.NativeObject;

                    using (DirectorySearcher search = new DirectorySearcher(entry))
                    {
                        search.Filter = $"(&(objectClass=user)(sAMAccountName={user.Username}))";
                        search.PropertiesToLoad.AddRange(new[] { "givenName", "middleName", "sn", "mail" });

                        SearchResult result = search.FindOne();

                        if (result == null)
                        {
                            userInfo.Result = new Response.Result { Status = "NOTFOUND", Message = "User not found in AD" };
                            return userInfo;
                        } 
                        var @operator = _efDbContext.User!.FirstOrDefault(d => d.Username == user.Username) ?? new Model.User() { Id = 0 };

                        var gwUser = await _userService.ByUsername(user.Username);

                        if (gwUser != null && !string.IsNullOrEmpty(gwUser.Username))
                        {
                            userInfo.Id = gwUser.Id;
                            userInfo.Username = user.Username;
                            userInfo.FirstName = GetLdapValue(result, "givenName");
                            userInfo.MiddleName = GetLdapValue(result, "middleName");
                            userInfo.LastName = GetLdapValue(result, "sn");
                            userInfo.Email = GetLdapValue(result, "mail");
                            userInfo.Branch = gwUser.Branch;
                            userInfo.LDAPPath = ldapPath;
                            await _activeUserRepository.AddAsync(new Model.ActiveUser
                            {
                                UserId = @operator.Id,
                                Terminal = user.Terminal!.ToUpper(),
                                ActivityDate = DateTime.Now
                            });
                            userInfo.Result = new Response.Result { Status = "SUCCESS", Message = "User found" };
                        }
                        else
                        {
                            userInfo.Result = new Response.Result { Status = "NOTENROLLED", Message = "User not yet enrolled to Portal" };
                        }
                    }
                }
            }
            catch (DirectoryServicesCOMException ex)
            {
                userInfo.Result = new Response.Result { Status = "INVALID", Message = "Invalid Credentials or Account Locked" };
            }
            catch (Exception ex)
            {
                userInfo.Result = new Response.Result { Status = "ERROR", Message = "Authentication Service Unavailable" };
            }

            return userInfo;
        }
        public async Task<Response.User> LDAPUserDetails(string username)
        {
            Response.User userInfo = new();

            try
            {
                string ldapPath = $"LDAP://{_activeDirectoryDomain}";

                using (DirectoryEntry entry = new DirectoryEntry(ldapPath))
                {
                    using (DirectorySearcher search = new DirectorySearcher(entry))
                    {
                        search.Filter = $"(&(objectClass=user)(sAMAccountName={username}))";

                        search.PropertiesToLoad.Add("givenName");
                        search.PropertiesToLoad.Add("middleName");
                        search.PropertiesToLoad.Add("sn");
                        search.PropertiesToLoad.Add("mail");

                        SearchResult result = search.FindOne();

                        if (result != null)
                        {
                            userInfo.Username = username;
                            userInfo.FirstName = GetProperty(result, "givenName");
                            userInfo.MiddleName = GetProperty(result, "middleName");
                            userInfo.LastName = GetProperty(result, "sn");
                            userInfo.Email = GetProperty(result, "mail");
                        }
                    }
                }
                return userInfo;
            }
            catch (Exception ex)
            {
                userInfo.Result = new Response.Result { Status = "ERROR", Message = "Search failed" };
                return userInfo;
            }
        }
        private string GetProperty(SearchResult result, string propName)
        {
            return result.Properties.Contains(propName) && result.Properties[propName].Count > 0
                   ? result.Properties[propName][0].ToString()
                   : string.Empty;
        } 
        public async Task<bool> CheckPasswordExpired(string username, string path)
        {
            try
            {
                using (DirectoryEntry entry = new DirectoryEntry(path))
                {
                    using (DirectorySearcher search = new DirectorySearcher(entry!))
                    {
                        search.Filter = $"(&(objectClass=user)(sAMAccountName={username}))";
                        SearchResult result = search.FindOne();
                        long pwdLastSetTicks = (long)result.Properties["pwdLastSet"][0]!;
                        DateTime pwdLastSet = DateTime.FromFileTime(pwdLastSetTicks);
                         
                        TimeSpan maxPwdAge = GetMaxPwdAge(entry);
                         
                        DateTime passwordExpirationDate = pwdLastSet.Add(maxPwdAge);
                         
                        if (passwordExpirationDate <= DateTime.Now)
                        {
                            return await Task.FromResult(true);
                        }
                        else
                        {
                            return await Task.FromResult(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return await Task.FromResult(false);
            }

        }
        private static TimeSpan GetMaxPwdAge(DirectoryEntry entry)
        {
            try
            {
                using DirectorySearcher searcher = new(entry);
                searcher.Filter = "(objectClass=domainDNS)";
                searcher.PropertiesToLoad.Add("maxPwdAge");

                var result = searcher.FindOne();

                if (result != null && result.Properties.Contains("maxPwdAge"))
                {
                    long maxPwdAgeTicks = (long)result.Properties["maxPwdAge"][0];
                    return TimeSpan.FromTicks(Math.Abs(maxPwdAgeTicks));
                }
                else
                {
                    Console.WriteLine("maxPwdAge attribute not found or could not be retrieved.");
                    return TimeSpan.Zero;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception retrieving maxPwdAge: {ex.Message}");
                return TimeSpan.Zero;
            }
        }
    }

}
