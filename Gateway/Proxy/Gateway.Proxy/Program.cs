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

// (Note: Ito ay para sa internal Admin Controllers ng Gateway kung meron, 
// ang Proxy JWT validation ay hina-handle sa SecurityMiddleware)
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

// [ CRITICAL FIX ] 
// Dapat mauna ang UseRouting para mabasa ng SecurityMiddleware ang YARP Route Metadata
app.UseRouting();

app.UseMiddleware<SecurityMiddleware>();
app.UseMiddleware<AuditMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<ResponseCachingMiddleware>();

app.UseHealthChecks("/health");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// =========================================================================
// EMBEDDED CENTRAL AUTH SERVICE (Token Generator)
// Bypass nito si YARP, Gateway na mismo ang magi-issue ng JWT Token.
// =========================================================================
app.MapPost("/api/v1/auth/token", async (HttpContext context, IClientService clientService, EFDbContext dbContext) =>
{
    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
    var bodyText = await reader.ReadToEndAsync();

    string clientCode = "", clientSecret = "", providerCode = "DEFAULT";

    if (!string.IsNullOrWhiteSpace(bodyText))
    {
        try
        {
            using var doc = JsonDocument.Parse(bodyText);
            var root = doc.RootElement;

            if (root.TryGetProperty("client_id", out var c) || root.TryGetProperty("code", out c)) clientCode = c.GetString() ?? "";
            if (root.TryGetProperty("client_secret", out var s) || root.TryGetProperty("secret", out s)) clientSecret = s.GetString() ?? "";

            // Optional: Kung ang client gusto pumili kung anong token provider gagamitin niya.
            if (root.TryGetProperty("provider_code", out var p)) providerCode = p.GetString() ?? "DEFAULT";
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { message = "Invalid JSON payload." });
        }
    }

    if (string.IsNullOrEmpty(clientCode) || string.IsNullOrEmpty(clientSecret))
    {
        return Results.BadRequest(new { message = "Client ID and Client Secret are required." });
    }

    // 1. Verify API Key/Credentials sa Database
    var client = await clientService.ByUsernameAndPassword(clientCode, clientSecret);
    if (client == null)
    {
        return Results.Unauthorized();
    }

    // 2. Kunin ang AuthProvider Configuration sa DB
    var authProvider = await dbContext.Set<AuthProvider>()
        .AsNoTracking()
        .FirstOrDefaultAsync(a => (a.Code == providerCode && a.IsActive))
        ?? await dbContext.Set<AuthProvider>().AsNoTracking().FirstOrDefaultAsync(a => a.IsActive);

    if (authProvider == null)
    {
        return Results.Problem("System Error: No active Auth Provider configured in the Gateway Database.");
    }

    // 3. Generate JWT Token gamit ang Settings ng napiling AuthProvider
    string token = JwtHelper.GenerateToken(client.Code, authProvider);

    return Results.Ok(new
    {
        access_token = token,
        token_type = "Bearer",
        expires_in = authProvider.TokenLifetimeMinutes * 60,
        provider = authProvider.Name
    });
}); 

app.MapReverseProxy();

app.Run();