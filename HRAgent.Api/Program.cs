using HRAgent.Api.Services;
using HRAgent.Api.Services;
using HRAgent.Api.Services.IService;
using Serilog;

var basePath = AppContext.BaseDirectory;
var logFilePath = Path.Combine(basePath, "Logs", "HRAgent-.txt");

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

    Log.Information("Starting HRAgent Microservice...");

    builder.Services.AddMemoryCache();
    builder.Host.UseSerilog();

    //builder.Services.AddScoped<IAuthenticateService, AuthenticateService>();
    builder.Services.AddScoped<IRecruitmentService, RecruitmentService>();
    builder.Services.AddScoped<IAssistantService, AssistantService>();
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