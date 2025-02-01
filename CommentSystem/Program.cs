using CommentSystem.Data;
using CommentSystem.GraphQL;
using CommentSystem.Messaging.Consumers;
using CommentSystem.Messaging.Interfaces;
using CommentSystem.Messaging.Producers;
using CommentSystem.Repositories.Implementations;
using CommentSystem.Repositories.Interfaces;
using CommentSystem.Services.Implementations;
using CommentSystem.Services.Interfaces;
using CommentSystem.WebSockets;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using RabbitMQ.Client;
using Serilog;
using CommentSystem.Helpers;
using CommentSystem.Models.Inputs;
using CommentSystem.Config;
using Common.Middleware;
using Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

ConfigureLogging(builder);

var appOptions = LoadAppOptions(builder);
await ConfigureServicesAsync(builder.Services, appOptions);

builder.Services.ConfigureRateLimiting(appOptions, opts => opts.IpRateLimit);

var app = builder.Build();
await ConfigureMiddleware(app);
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
        var errorMsg = "Missing AppOptions configuration in CommentSystem appsettings.json";
        Log.Fatal(errorMsg);
        throw new InvalidOperationException(errorMsg);
    }

    builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("AppOptions"));
    return appOptions;
}

/// <summary>
/// Register services in DI container
/// </summary>
async Task ConfigureServicesAsync(IServiceCollection services, AppOptions options)
{
    // MSSQL
    services.AddDbContext<ApplicationDbContext>(dbOptions =>
        dbOptions.UseSqlServer(options.ConnectionStrings.DefaultConnection));

    // RabbitMQ
    var factory = new ConnectionFactory
    {
        HostName = options.RabbitMq.HostName,
        UserName = options.RabbitMq.UserName,
        Password = options.RabbitMq.Password,
        Port = options.RabbitMq.Port
    };

    var rabbitConnection = await factory.CreateConnectionAsync();
    services.AddSingleton<IConnection>(rabbitConnection);
    services.AddSingleton<IRabbitMqProducer, RabbitMqProducer>();
    services.AddHostedService<RabbitMqConsumer>();

    // Repositories
    services.AddScoped<ICommentRepository, CommentRepository>();

    // Services
    services.AddScoped<ICommentService, CommentService>();
    services.AddScoped<IRemoteCaptchaService, RemoteCaptchaService>();
    services.AddHttpClient<IFileServiceApiClient, FileServiceApiClient>();

    // GraphQL
    services
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .AddMutationType<Mutation>()
        .AddFiltering()
        .AddSorting()
        .AddInstrumentation();

    services.AddValidation();
    services.AddScoped<IValidator<AddCommentInput>, AddCommentInputValidator>();

    // Redis
    services.AddStackExchangeRedisCache(redisOptions =>
    {
        redisOptions.Configuration = options.Redis.Connection;
        redisOptions.InstanceName = options.Redis.InstanceName;
    });

    services.AddSignalR();
}

/// <summary>
/// Configure middleware and routing
/// </summary>
async Task ConfigureMiddleware(WebApplication app)
{
    // Proxing IP
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    // Request logging
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseRequestLogging();

    // Auto-migrate database in development
    if (app.Environment.IsDevelopment())
    {
        await ApplyMigrationsAsync(app);
    }

    // Metrics and routing
    app.UseHttpMetrics();
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthorization();
    app.MapHub<WebSocketHub>("/ws"); // WebSocket
    app.MapGraphQL();
    app.MapMetrics(); // Prometheus
}

/// <summary>
/// Apply database migrations
/// </summary>
async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}
