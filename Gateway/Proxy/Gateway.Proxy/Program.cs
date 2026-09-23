using Gateway.BLL;
using Gateway.BLL.Helper;
using Gateway.BLL.Services;
using Gateway.BLL.Services.IService;
using Gateway.Data.Models;
using Gateway.Proxy.Helper; // <-- Master Cache Signal Helper
using Gateway.Proxy.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Text.Json;
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

// =========================================================================
// DYNAMIC MULTI-ISSUER / CENTRALIZED GATEWAY JWT AUTHENTICATION
// =========================================================================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var internalSecret = builder.Configuration["JWT:Secret"]!;
        var internalIssuer = builder.Configuration["JWT:Issuer"];
        var internalAudience = builder.Configuration["JWT:Audience"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(internalSecret)),

            ValidateIssuer = true,
            ValidIssuer = internalIssuer,

            IssuerValidator = (issuer, securityToken, validationParameters) =>
            {
                if (issuer == internalIssuer)
                {
                    return issuer;
                }

                if (!string.IsNullOrEmpty(issuer))
                {
                    return issuer;
                }

                throw new SecurityTokenInvalidIssuerException("Invalid or untrusted Token Issuer.");
            },

            ValidateAudience = !string.IsNullOrEmpty(internalAudience),
            ValidAudience = internalAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Warning("JWT Validation notice: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.Configure<CertificateOptions>(builder.Configuration.GetSection("Certificate"));
builder.Services.AddDbContext<EFDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("Default")!));
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

// [ CRITICAL FIX ] UseRouting bago ang Middlewares
app.UseRouting();

// Custom Gateway Middlewares
app.UseMiddleware<AuditMiddleware>();
app.UseMiddleware<SecurityMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<ResponseCachingMiddleware>();

app.UseHealthChecks("/health");

// INTERNAL RELOAD ENDPOINT FOR PUBLISH ACTION
app.MapPost("/internal/gateway/reload", (
    IMemoryCache cache,
    DatabaseProxyConfigProvider proxyConfigProvider) =>
{
    // 1. Explicitly burahin ang specific key sa RAM
    cache.Remove("GATEWAY_ACTIVE_ROUTES");

    // 2. Flush master change tokens
    GatewayCacheSignal.ResetAllCaches();

    // 3. I-rebuild ang YARP Endpoints
    proxyConfigProvider.UpdateConfig();

    return Results.Ok(new { status = "SUCCESS", message = "Gateway Proxy memory and YARP routes reloaded successfully." });
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapReverseProxy();

app.Run();