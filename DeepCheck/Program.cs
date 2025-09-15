using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using DeepCheck.Data;
using DeepCheck.Interfaces;
using DeepCheck.Middlewares;
using DeepCheck.Repositories;
using DeepCheck.Services;
using DeepCheck.Services.Jobs;
using DeepCheck.Services.Puppeteer;
using DeepCheck.Services.TestRunService;
using Hangfire;
using Hangfire.MemoryStorage;
using DeepCheck.Helpers;
using DeepCheck.Services.JobCleanup;
using DeepCheck.Services.Ttws;
using Serilog.Logfmt;
using DeepCheck.Hubs;

var builder = WebApplication.CreateBuilder(args);

var formatter = new LogfmtFormatter(opts => opts.IncludeAllProperties().OnException(e => e
    .LogExceptionData(                          // still log type+message
        LogfmtExceptionDataFormat.Type |
        LogfmtExceptionDataFormat.Message
    ).LogStackTrace(LogfmtStackTraceFormat.SingleLine)));  // ← this adds the full stack trace

Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration) // Read configuration from appsettings.json
            .Enrich.FromLogContext()
            .WriteTo.Console(formatter) // Log to console
            .WriteTo.File(path: "logs/log-.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, retainedFileCountLimit: 3, formatter: formatter)
            .CreateLogger();

builder.Host.UseSerilog();

var services = builder.Services;

// Add services to the container.
services.AddProblemDetails(options =>
{
    // Customize all ProblemDetails produces by the app
    options.CustomizeProblemDetails = ctx =>
    {
        var traceId = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
        ctx.ProblemDetails.Extensions["traceId"] = traceId;
    };
});

// Register a single global handler (can register more if you need special pipelines)
services.AddExceptionHandler<GlobalExceptionHandler>();

//Hangfire
builder.Services.AddHangfire(config =>
{
    config.UseMemoryStorage();
    config.UseFilter<AutomaticRetryAttribute>(new AutomaticRetryAttribute { Attempts = 1 });
});

builder.Services.AddHangfireServer();

services.AddControllers()
  .AddJsonOptions(options =>
  {
      options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
      options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
  });
services.AddOpenApi();
services.AddSwaggerGen();

services.AddHostedService<BackgroundJobSchedulerHostedService>();
services.AddMemoryCache();
services.AddHttpClient();
services.AddSingleton<ITtwsClient, TtwsClient>();
// SignalR for uptime heartbeat
services.AddSignalR();

services.AddSingleton<ITest, WsUserLoginAndMarketOverview>();
services.AddSingleton<ITest, TtwsResponsivenessCheck>();
// services.AddSingleton<ITest, PushTestJob>();

services.AddScoped<ITestRepository, TestRepository>();
services.AddScoped<ITestRunService, TestRunService>();
services.AddScoped<IDeepCheckInfoService, DeepCheckInfoService>();
services.AddScoped<IJobCleanupService, JobCleanupService>();
services.AddSingleton<IBrowserProvider, BrowserProvider>();
services.AddSingleton<IPuppeteerService, PuppeteerService>();
// services.AddSingleton<IJobCleanupService, JobCleanupService>();
// Test runner orchestrates all tests and persists results
services.AddScoped<ITestRunner, TestRunner>();
services.Decorate<ITestRunner, HangfireExclusiveTestRunner>();

services.Configure<PuppeteerSettings>(builder.Configuration.GetSection("PuppeteerSettings"));
services.Configure<WsChecksSettings>(builder.Configuration.GetSection("WsChecksSettings"));
services.Configure<PushSubscriptionCheckSettings>(builder.Configuration.GetSection("PushSubsciptionCheckSettings"));
services.Configure<TtwsResponsivenessCheckSettings>(builder.Configuration.GetSection("TtwsResponsivenessCheckSettings"));
services.Configure<KumaServiceSettings>(builder.Configuration.GetSection("KumaServiceSettings"));
services.Configure<JobCleanupSettings>(builder.Configuration.GetSection("JobCleanupSettings"));

// Database
var connectionString = builder.Configuration.GetConnectionString("DeepCheckDb");
Log.Logger.Information("Connection string: {connectionString}", connectionString);

services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(connectionString!); // Stores database as "app.db" in your app folder
});

var app = builder.Build();

// Run migrations at startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.UseExceptionHandler();

app.UseSerilogRequestLogging(); // Logs requests

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHangfireDashboard("/hangfire", new DashboardOptions()
{
    IsReadOnlyFunc = context => !app.Environment.IsDevelopment()
});

// Serve static files for the uptime page and expose a simple redirect
app.UseStaticFiles();
app.MapHub<UptimeHub>("/uptime-hub");
app.MapGet("/uptime", () => Results.Redirect("/uptime/index.html"));

app.MapControllers();

app.Run();
