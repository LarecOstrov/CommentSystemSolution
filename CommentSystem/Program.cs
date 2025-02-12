using Common.Data;
using Common.GraphQL;
using Common.Messaging.Interfaces;
using Common.Repositories.Implementations;
using Common.Repositories.Interfaces;
using Common.Services.Implementations;
using Common.Services.Interfaces;
using Common.WebSockets;
using Common.Messaging.Producers;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using RabbitMQ.Client;
using Serilog;
using Common.Models.Inputs;
using Common.Config;
using Common.Middlewares;
using Common.Extensions;
using Common.Helpers;
using Common.Repositories.Implementation;
using Common.Models;

var builder = WebApplication.CreateBuilder(args);

// Load AppOptions from Common
var appOptions = LoadAppOptionsHelper.LoadAppOptions(builder);

ConfigureLogging(builder);

await ConfigureServicesAsync(builder.Services, appOptions);

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
async Task ConfigureServicesAsync(IServiceCollection services, AppOptions options)
{
    // MSSQL
    Log.Information($"Connection MSSQL: {options.ConnectionStrings.DefaultConnection}");
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

    Log.Information($"Connecting to RabbitMQ at {factory.HostName}:{factory.Port}, {factory.UserName} {factory.Password}");
    services.AddSingleton<IConnectionFactory>(factory);
    var rabbitConnection = await factory.CreateConnectionAsync();
    services.AddSingleton<IConnection>(rabbitConnection);
    services.AddSingleton<IRabbitMqProducer, RabbitMqProducer>();

    // Add Controllers
    services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
        });
    // Repositories
    services.AddScoped<ICommentRepository, CommentRepository>();
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IFileAttachmentRepository, FileAttachmentRepository>();

    // Services
    services.AddScoped<ICommentService, CommentService>();
    services.AddScoped<ICaptchaCacheService, CaptchaCacheService>();
    services.AddScoped<IFileStorageService, FileStorageService>();
    services.AddScoped<IFileAttachmentService, FileAttachmentService>();

    // GraphQL
    services
    .AddGraphQLServer()
    .ModifyRequestOptions(opt =>
    {
        opt.IncludeExceptionDetails = true; 
        opt.ExecutionTimeout = TimeSpan.FromSeconds(30); 
    })
    .ModifyOptions(opt =>
    {
        opt.StrictValidation = true; 
    })
    .UseRequest(next => async context =>
    {
        var complexity = context.ContextData.TryGetValue("query-complexity", out var value) ? value : "Unknown";
        Console.WriteLine($"GraphQL Query Complexity: {complexity}");
        await next(context);
    })
    .AddQueryType<Query>()
    .AddType<Comment>()
    .AddType<User>()
    .AddType<FileAttachment>()
    .AddFiltering()
    .AddSorting()
    .AddInstrumentation()
    .AddProjections()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true);

    services.AddScoped<Query>();

    services.AddValidation();
    services.AddScoped<IValidator<CommentInput>, AddCommentInputHelper>();

    // Redis
    services.AddStackExchangeRedisCache(redisOptions =>
    {
        redisOptions.Configuration = options.Redis.Connection;
        redisOptions.InstanceName = options.Redis.InstanceName;
    });

    services.AddSignalR();

    // CORS Configuration
    var corsOptions = options.Cors;
    services.AddCors(options =>
    {
        if (corsOptions?.CommentService.AllowedOrigins?.Any() == true)
        {
            options.AddPolicy("AllowSpecificOrigins", builder =>
                builder.WithOrigins(corsOptions.CommentService.AllowedOrigins)
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
/// Configure middleware and routing
/// </summary>
async Task ConfigureMiddleware(WebApplication app, AppOptions appOptions)
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
        await MigrationHelper.ApplyMigrationsAsync(app, appOptions);
    }

    // Metrics and routing
    app.UseHttpMetrics();
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthorization();
    app.MapHub<WebSocketHub>("/ws"); // WebSocket
    app.MapGraphQL();
    app.MapMetrics(); // Prometheus

    // Enable CORS
    app.UseCors(appOptions.Cors.CommentService.AllowedOrigins.Any() ? "AllowSpecificOrigins" : "AllowAll");

    app.MapControllers();
}
