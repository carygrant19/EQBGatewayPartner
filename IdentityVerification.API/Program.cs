using Common.Handlers;
using Common.Services;
using IdentityVerification.API.Services;
using IdentityVerification.API.Services.IService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog; 
using System.Text;
using System.Text.Json;

var basePath = AppContext.BaseDirectory;
var logFilePath = Path.Combine(basePath, "Logs", "IdentityService-.txt");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Logger Configuration
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console()
        .WriteTo.File(
            logFilePath,
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

    Log.Information("Starting Identity Microservice...");
    builder.Host.UseSerilog();

    // CORS Configuration
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", policy =>
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials());
    });

    // OpenTelemetry Tracing Configuration
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(serviceName: "IdentityVerification.API", serviceVersion: "1.0.0"));

            // Uncomment as needed:
            tracing.AddAspNetCoreInstrumentation(options => options.RecordException = true);
            tracing.AddHttpClientInstrumentation();

            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
        });

    // JWT Authentication
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

    // Service Dependency Injection 
    builder.Services.AddScoped<INidasService, NidasService>();

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddOutboundTransactionLogging();

    builder.Services.AddHttpClient("ExternalClient")
        .AddHttpMessageHandler<OutboundLoggingHandler>();


    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = "Gateway Partner API";
            document.Info.Version = "v1";

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter JWT Bearer token",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = securityScheme;

            return Task.CompletedTask;
        });
    });
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Gateway Partner API Reference")
                   .WithTheme(ScalarTheme.Purple);
        });
    }

    app.UseHealthChecks("/health");
    app.UseHttpsRedirection();

    // Middleware Pipeline Order
    app.UseCors("CorsPolicy");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Microservice terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}