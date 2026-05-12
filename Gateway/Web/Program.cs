using Gateway.Data.Models;
using Gateway.Helper; 
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using Gateway.BLL;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;
builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var systemParameters = builder.Configuration.GetSection("SystemParameters").Get<SystemParameters>();

builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddDbContext<EFDbContext>(options =>
{
    options.UseSqlServer(configuration.GetConnectionString("Default")!);
    options.EnableSensitiveDataLogging();
});

builder.Services.GatewayServices();
builder.Services.HelperServices(configuration);

builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

builder.Services.AddControllers().AddNewtonsoftJson((options =>
{ 
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore; 
}));

builder.Services.AddRazorPages();
builder.Services.AddAntiforgery(o => o.HeaderName = "XSRF-TOKEN");
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "EQBGateway.session";
    if (systemParameters != null)
        options.IdleTimeout = TimeSpan.FromMinutes(Convert.ToDouble(systemParameters.SessionTimeOutMinutes));
    //options.IdleTimeout = TimeSpan.FromSeconds(20);
    options.Cookie.IsEssential = true;
    options.Cookie.HttpOnly = true;
    //options.Cookie.Domain = "swift.com";
    //options.Cookie.Path = "/";
    //options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    //options.Cookie.HttpOnly = true;
});

builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Login"; // Redirects to this path if not authenticated 
    });

builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

    //Para sa camelcasing, uncomment para pwede naka Capital ang return ng json
    //options.JsonSerializerOptions.PropertyNamingPolicy = null;
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.MapFallbackToPage("/Login");
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
    // Check if the request is targeting the "css" folder
    if (context.Request.Path.StartsWithSegments("/css", StringComparison.OrdinalIgnoreCase))
    {
        // Allow only GET and HEAD methods
        if (!HttpMethods.IsGet(context.Request.Method) &&
            !HttpMethods.IsHead(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return; // Terminate request here
        }
    }
    await next();
});
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.UseEndpoints(endpoints =>
{
    _ = endpoints.MapRazorPages();
});

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

app.Run();
