using Common.Config;
using Common.Data;
using Common.Extensions;
using Common.Helpers;
using Common.Middlewares;
using Common.Repositories.Implementations;
using Common.Repositories.Interfaces;
using Common.Services.Implementations;
using Common.Services.Interfaces;
using FileServiceAPI.Services.Implementations;
using FileServiceAPI.Services.Interfaces;
using FileServiceAPI.Workers;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

ConfigureLogging(builder);

var appOptions = LoadAppOptionsHelper.LoadAppOptions();
ConfigureServices(builder, appOptions);

builder.Services.ConfigureRateLimiting(appOptions, opts => opts.IpRateLimit.FileService);

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
    var corsOptions = appOptions.Cors;
    builder.Services.AddCors(options =>
    {
        if (corsOptions?.FileService.AllowedOrigins?.Any() == true)
        {
            options.AddPolicy("AllowSpecificOrigins", builder =>
                builder.WithOrigins(corsOptions.FileService.AllowedOrigins)
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
        await MigrationHelper.ApplyMigrationsAsync(app, appOptions);
    }
    // Swagger for development
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Enable CORS
    app.UseCors(appOptions.Cors.FileService.AllowedOrigins.Any() ? "AllowSpecificOrigins" : "AllowAll");

    app.UseAuthorization();
    app.MapControllers();
}
