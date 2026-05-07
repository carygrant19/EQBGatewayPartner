using Serilog;
using Temenos.API.Sevices;
using Temenos.API.Sevices.IService;

var basePath = AppContext.BaseDirectory;
var logFilePath = Path.Combine(basePath, "Logs", "TemenosService-.txt");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(logFilePath,
        rollingInterval: RollingInterval.Day, 
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Temenos Microservice...");

    var builder = WebApplication.CreateBuilder(args);

    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", builder =>
            builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .Build());
    });
    builder.Services.AddScoped<IAuthenticateService, AuthenticateService>();
    builder.Services.AddScoped<ITransactionService, TransactionService>();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Host.UseSerilog();

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