using Gateway.BLL;
using Gateway.BLL.Helper;
using Gateway.BLL.Services;
using Gateway.BLL.Services.IService;
using Gateway.Data.Models;
using Gateway.Proxy.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

var mailSection = builder.Configuration.GetSection("MailSettings");
var mailSettings = mailSection.Get<MailSettings>()
    ?? throw new Exception("CRITICAL: 'MailSettings' section is missing in appsettings.json!");

builder.Services.AddSingleton(mailSettings);
builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]!)),
            ValidAudience = builder.Configuration["JWT:Audience"],
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.Configure<CertificateOptions>(builder.Configuration.GetSection("Certificate"));
builder.Services.AddDbContext<EFDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("Default")!));
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 104857600);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 104857600;
    options.ConfigureHttpsDefaults(o => o.ClientCertificateMode = ClientCertificateMode.AllowCertificate);
});

builder.Services.AddMemoryCache();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile(new MappingProfile(builder.Configuration));
});

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.ProxyServices();
builder.Services.AddReverseProxy();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.All });
app.UseMiddleware<SecurityMiddleware>();
app.UseMiddleware<AuditMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<ResponseCachingMiddleware>();
app.UseHealthChecks("/health");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapReverseProxy();

app.Run();