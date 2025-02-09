using Common.Config;
using CaptchaServiceAPI.Services.Implementations;
using CaptchaServiceAPI.Services.Interfaces;
using Common.Extensions;
using Common.Middlewares;
using Common.Services.Implementations;
using Common.Services.Interfaces;
using DNTCaptcha.Core;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Common.Helpers;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Завантаження загальної конфігурації AppOptions

var appOptions = LoadAppOptionsHelper.LoadAppOptions(builder);

ConfigureLogging(builder);
ConfigureServices(builder, appOptions);

builder.Services.ConfigureRateLimiting(appOptions, opts => opts.IpRateLimit.CaptchaService);

var app = builder.Build();
ConfigureMiddleware(app, appOptions);

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
void ConfigureServices(WebApplicationBuilder builder, AppOptions appOptions)
{
    // Captcha Service
    builder.Services.AddScoped<ICaptchaService, CaptchaService>();
    builder.Services.AddScoped<ICaptchaCacheService, CaptchaCacheService>();
    builder.Services.AddSingleton<ICaptchaCryptoProvider, CaptchaCryptoProvider>();
    builder.Services.AddSingleton<ICaptchaImageProvider, CaptchaImageProvider>();

    // Controllers
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Swagger
    builder.Services.AddSwaggerGen();

    // Redis Configuration for saving captcha keys and values
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = appOptions.Redis.Connection;
        options.InstanceName = appOptions.Redis.InstanceName;
    });

    // DNTCaptcha Configuration
    builder.Services.AddDNTCaptcha(options =>
    {
        options.UseDistributedCacheStorageProvider(); // Use Distributed Cache
        options.ShowThousandsSeparators(false);
        options.WithEncryptionKey(appOptions.CaptchaSettings.EncryptionKey);
    });

    // CORS Configuration
    var corsOptions = appOptions.Cors;
    builder.Services.AddCors(options =>
    {
        if (corsOptions?.CaptchaService.AllowedOrigins?.Any() == true)
        {
            options.AddPolicy("AllowSpecificOrigins", builder =>
                builder.WithOrigins(corsOptions.CaptchaService.AllowedOrigins)
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
void ConfigureMiddleware(WebApplication app, AppOptions appOptions)
{
    // Proxing IP
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    // Middleware for logging
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseRequestLogging();

    // Swagger for development
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Enable CORS
    app.UseCors(appOptions.Cors.CaptchaService.AllowedOrigins.Any() ? "AllowSpecificOrigins" : "AllowAll");

    app.UseAuthorization();
    app.MapControllers();
}
