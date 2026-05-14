using Instapay.Api.Services;
using Instapay.Api.Services.IService;
using Common.Services;
using Common.Services.IService;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;



var basePath = AppContext.BaseDirectory;
var logFilePath = Path.Combine(basePath, "Logs", "InstapayService-.txt");



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

    Log.Information("Starting Instapay Microservice...");
    


    builder.Services.AddOpenTelemetry()
     .WithTracing(tracing =>
     {
         tracing
             .SetResourceBuilder(
                 ResourceBuilder.CreateDefault()
                     .AddService(serviceName: "Instapay.API", serviceVersion: "1.0.0"))

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
    builder.Services.AddScoped<ITransactionService, TransactionService>();
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