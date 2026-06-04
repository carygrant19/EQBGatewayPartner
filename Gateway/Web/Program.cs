using Gateway.Data.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using Gateway.BLL;
using Gateway.BLL.Helper;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;
 
builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var systemParameters = builder.Configuration.GetSection("SystemParameters").Get<SystemParameters>();
 
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        // Para sa camelcasing, uncomment para pwede naka Capital ang return ng json
        // options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
    });
 
builder.Services.AddDbContext<EFDbContext>(options =>
{
    options.UseSqlServer(configuration.GetConnectionString("Default")!);
    options.EnableSensitiveDataLogging();
});
 
builder.Services.HelperServices(configuration);
builder.Services.PortalServices();
 
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery(o => o.HeaderName = "XSRF-TOKEN");
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
 
 

builder.Services.AddHealthChecks();
 
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Login"; 
    });
builder.Services.AddAuthorization();
 
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "EQBGateway.session";
    if (systemParameters != null)
    {
        options.IdleTimeout = TimeSpan.FromMinutes(Convert.ToDouble(systemParameters.SessionTimeOutMinutes));
    }
    options.Cookie.IsEssential = true;
    options.Cookie.HttpOnly = true;
});

builder.Services.AddAutoMapper(cfg =>
{ 
    cfg.AddProfile(new MappingProfile(configuration));
});
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePages(context =>
{
    if (context.HttpContext.Response.StatusCode == 400)
    {
        context.HttpContext.Response.Redirect("/Error?code=400");
    }
    else if (context.HttpContext.Response.StatusCode == 404)
    {
        context.HttpContext.Response.Redirect("/Error?code=404");
    }
    return Task.CompletedTask;
});

app.UseHttpsRedirection();
 
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/css", StringComparison.OrdinalIgnoreCase))
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }
    }
    await next();
});

app.UseStaticFiles();  

app.UseRouting();      

app.UseSession();      

app.UseAuthentication();  
app.UseAuthorization();
 
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    context.Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate");
    context.Response.Headers.Append("Pragma", "no-cache");
    context.Response.Headers.Append("Expires", "0");

    await next();
});
 
app.MapRazorPages()
   .WithStaticAssets();  
 
app.MapFallbackToPage("/Login");

app.Run();