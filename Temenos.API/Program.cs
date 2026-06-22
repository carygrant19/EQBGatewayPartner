using Serilog;
using Common.Services;
using Common.Services.IService;
using v1 = Temenos.API.Services.v1;
using iv1 = Temenos.API.Services.IService.v1;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var basePath = AppContext.BaseDirectory;
var logFilePath = Path.Combine(basePath, "Logs", "TemenosService-.txt");

try
{

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", builder =>
            builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .Build());
    });

    Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(logFilePath,
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

    Log.Information("Starting Temenos Microservice...");

    builder.Services.AddOpenTelemetry()
      .WithTracing(tracing =>
      {
          tracing
              .SetResourceBuilder(
                  ResourceBuilder.CreateDefault()
                      .AddService(serviceName: "Temenos.API", serviceVersion: "1.0.0"))

              .AddAspNetCoreInstrumentation(options =>
              {
                  options.RecordException = true;
              })
              .AddHttpClientInstrumentation()

              .AddSqlClientInstrumentation(options =>
              {
                  options.RecordException = true;
              })

              .AddOtlpExporter(options =>
              {
                  options.Endpoint = new Uri("http://localhost:4317");
                  options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
              });
      });

    builder.Host.UseSerilog();
    builder.Services.AddScoped<IAuthenticateService, AuthenticateService>();
    builder.Services.AddScoped<iv1.ITransactionService, v1.TransactionService>();
    builder.Services.AddScoped<iv1.IAccountService, v1.AccountService>();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();

    builder.Services.AddHealthChecks();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHealthChecks("/health");
   
    app.UseHttpsRedirection();

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