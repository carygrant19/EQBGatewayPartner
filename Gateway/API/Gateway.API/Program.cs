using Gateway.Data.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using Gateway.BLL;
using Gateway.BLL.Helper;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

// 1. Maintain exact application settings configurations mapping
builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// --- DYNAMIC CORS CONFIGURATION FROM APPSETTINGS ---
string corsOriginsSetting = builder.Configuration.GetValue<string>("AppSettings:CorsOrigins") ?? "";
string[] allowedOrigins = corsOriginsSetting
    .Split(',', StringSplitOptions.RemoveEmptyEntries)
    .Select(origin => origin.Trim())
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("VueCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required because you are using cookies for authentication below
    });
});
// ---------------------------------------------------

// Extract System Parameters section to configure downstream session timeouts dynamically
var systemParameters = builder.Configuration.GetSection("SystemParameters").Get<SystemParameters>();

// 2. Map original JSON configurations (Handles both System.Text and Newtonsoft formats)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    })
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
    });

builder.Services.AddOpenApi();

// 3. Connect to Entity Framework using your exact connection string key ("Default")
builder.Services.AddDbContext<EFDbContext>(options =>
{
    options.UseSqlServer(configuration.GetConnectionString("Default")!);
    options.EnableSensitiveDataLogging(); // Keeps deep query debugging active
});

// 4. CRITICAL: Invoke your existing custom bulk dependency injectors
builder.Services.HelperServices(configuration);
builder.Services.PortalServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();

// 5. Setup authentication with API behavioral rules instead of Page redirects
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "EQBGateway.AuthCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.None; // Allows secure cookie passing across different ports (52127 -> 7176)

        // Prevent .NET from trying to redirect API calls to a non-existent server-side /Login page
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// 6. Bind your exact session parameters and naming conventions
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "EQBGateway.session";
    if (systemParameters != null)
    {
        options.IdleTimeout = TimeSpan.FromMinutes(Convert.ToDouble(systemParameters.SessionTimeOutMinutes));
    }
    else
    {
        options.IdleTimeout = TimeSpan.FromMinutes(60); // Safe fallback default
    }
    options.Cookie.IsEssential = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None; // Matches Auth Cookie cross-origin behavior
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 7. Core execution pipeline ordering rules
app.UseRouting();

// ENABLE CORS EXACTLY HERE
app.UseCors("VueCorsPolicy");

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// 8. Re-inject your exact security headers & anti-caching engine rules
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    context.Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate");
    context.Response.Headers.Append("Pragma", "no-cache");
    context.Response.Headers.Append("Expires", "0");
    await next();
});

// Map Controller endpoints dynamically (Allows Vite proxy to discover /api/auth/login)
app.MapControllers();

app.Run();