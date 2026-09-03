
using Gateway.BLL;
using Gateway.BLL.Services;
using Gateway.Data.Models;
using Gateway.BLL.Helper;
using Gateway.Proxy.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Values;
using Serilog;
using System.Text;
var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true) // This loads your routes!
    .AddEnvironmentVariables();

var mailSection = builder.Configuration.GetSection("MailSettings"); 
var mailSettings = mailSection.Get<MailSettings>(); 
if (mailSettings == null)
{
    throw new Exception("CRITICAL: 'MailSettings' section is missing in appsettings.json!");
} 
builder.Services.AddSingleton(mailSettings);
builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration));

//uncomment to enable bearer token validation in ocelot
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
builder.Services.Configure<OcelotCustomFileConfiguration>(builder.Configuration);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 104857600;
    options.ConfigureHttpsDefaults(o => o.ClientCertificateMode = ClientCertificateMode.AllowCertificate);
});

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile(new MappingProfile(builder.Configuration));
});

builder.Services.AddOcelot();

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.ProxyServices();
//builder.Services.AddTransient<IMailerService, MailerService>(); 
var app = builder.Build();

// 5. Middleware Pipeline
app.UseMiddleware<SecurityMiddleware>(); // 1st: Security
app.UseMiddleware<AuditMiddleware>();

app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.All });
app.UseHealthChecks("/health");
 
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 6. Ocelot Execution (Last in pipeline)
// Passing a configuration object here allows for runtime updates
await app.UseOcelot();

app.Run();