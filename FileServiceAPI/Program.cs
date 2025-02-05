using Common.Data;
using Common.Extensions;
using Common.Middlewares;
using Common.Repositories.Implementations;
using Common.Repositories.Interfaces;
using Common.Services.Implementations;
using Common.Services.Interfaces;
using FileServiceAPI.Config;
using FileServiceAPI.Services.Implementations;
using FileServiceAPI.Services.Interfaces;
using FileServiceAPI.Workers;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

ConfigureLogging(builder);

var appOptions = LoadAppOptions(builder);
ConfigureServices(builder, appOptions);

builder.Services.ConfigureRateLimiting(appOptions, opts => opts.IpRateLimit);

var app = builder.Build();
await ConfigureMiddleware(app, appOptions);


app.Run();

/// <summary>
/// Configure logger
/// </summary>
void ConfigureLogging(WebApplicationBuilder builder)
{
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger();

    builder.Host.UseSerilog();
}


/// <summary>
/// Load AppOptions from configuration
/// </summary>
AppOptions LoadAppOptions(WebApplicationBuilder builder)
{
    var appOptions = builder.Configuration.GetSection("AppOptions").Get<AppOptions>();
    if (appOptions == null)
    {
        var errorMsg = "Missing AppOptions configuration in FileServiceAPI appsettings.json";
        Log.Fatal(errorMsg);
        throw new InvalidOperationException(errorMsg);
    }

    builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("AppOptions"));
    Log.Warning($"DefaultConnection: {appOptions.ConnectionStrings.DefaultConnection}");
    return appOptions;
}

/// <summary>
/// Register services in DI container
/// </summary>
void ConfigureServices(WebApplicationBuilder builder, AppOptions options)
{
    
    // MSSQL
    builder.Services.AddDbContext<ApplicationDbContext>(dbOptions =>
        dbOptions.UseSqlServer(options.ConnectionStrings.DefaultConnection));

    // Services
    builder.Services.AddScoped<ICaptchaCacheService, CaptchaCacheService>();
    builder.Services.AddScoped<IFileStorageService, FileStorageService>();
    builder.Services.AddScoped<IFileAttachmentService, FileAttachmentService>();
    builder.Services.AddScoped<IOrphanFileCleanupService, OrphanFileCleanupService>();
    builder.Services.AddHostedService<OrphanFileCleanupWorker>();

    //Repository
    builder.Services.AddScoped<IFileAttachmentRepository, FileAttachmentRepository>();
    builder.Services.AddScoped<ICommentRepository, CommentRepository>();

    // Controllers
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    //Swagger
    builder.Services.AddSwaggerGen();

    // Redis Configuration for saving captcha keys and values
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = appOptions.Redis.Connection;
        options.InstanceName = appOptions.Redis.InstanceName;
    });

    // CORS Configuration
    var corsOptions = builder.Configuration.GetSection("CorsOptions").Get<CorsOptions>();
    builder.Services.AddCors(options =>
    {
        if (corsOptions?.AllowedOrigins?.Any() == true)
        {
            options.AddPolicy("AllowSpecificOrigins", builder =>
                builder.WithOrigins(corsOptions.AllowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader());
        }
        else
        {
            Log.Warning("CORS is misconfigured: No allowed origins specified.");
            options.AddPolicy("AllowAll",
                builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        }
    });
}

/// <summary>
/// Configure middleware
/// </summary>
async Task ConfigureMiddleware(WebApplication app, AppOptions appOptions)
{
    // Proxing IP (for Reverse Proxy)
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    // Middleware logging
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseRequestLogging();

    // Auto-migrate database in development
    if (app.Environment.IsDevelopment())
    {
        await ApplyMigrationsAsync(app);
    }
    // Swagger for development
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Enable CORS
    app.UseCors(appOptions.Cors.AllowedOrigins.Any() ? "AllowSpecificOrigins" : "AllowAll");

    app.UseAuthorization();
    app.MapControllers();
}

/// <summary>
/// Apply database migrations
/// </summary>
async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
    if (pendingMigrations.Any())
    {
        Log.Information("Applying {Count} pending migrations...", pendingMigrations.Count);
        await dbContext.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully.");
    }
    else
    {
        Log.Information("No pending migrations found.");
    }
}
