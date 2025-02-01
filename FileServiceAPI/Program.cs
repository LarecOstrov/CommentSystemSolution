﻿using FileServiceAPI.Config;
using FileServiceAPI.Services;
using Serilog;
using Microsoft.AspNetCore.HttpOverrides;
using FileServiceAPI.Services.Interfaces;
using Common.Middleware;
using Common.Extensions;

AppContext.SetSwitch("System.Drawing.EnableUnixSupport", true);

var builder = WebApplication.CreateBuilder(args);

ConfigureLogging(builder);

var appOptions = LoadAppOptions(builder);
ConfigureServices(builder, appOptions);

builder.Services.ConfigureRateLimiting(appOptions, opts => opts.IpRateLimit);

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
    return appOptions;
}

/// <summary>
/// Register services in DI container
/// </summary>
void ConfigureServices(WebApplicationBuilder builder, AppOptions appOptions)
{
    // Azure Blob Storage Service
    builder.Services.AddSingleton<IFileStorageService, AzureBlobService>();

    // Controllers
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    //Swagger
    builder.Services.AddSwaggerGen();

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
void ConfigureMiddleware(WebApplication app, AppOptions appOptions)
{
    // Proxing IP (for Reverse Proxy)
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    // Middleware logging
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseRequestLogging();
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
