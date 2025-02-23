using Common.Config;
using Common.Data;
using Common.Helpers;
using Common.Repositories.Implementation;
using Common.Repositories.Implementations;
using Common.Repositories.Interfaces;
using Common.Services.Implementations;
using Common.Services.Interfaces;
using Common.WebSockets;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Load AppOptions from Common
    var appOptions = LoadAppOptionsHelper.LoadAppOptions(builder);

    ConfigureLogging(builder);

    ConfigureServicesAsync(builder.Services, appOptions);

    var app = builder.Build();

    // Middleware pipeline setup
    app.UseRouting();

    // Allow CORS for WebSockets
    app.UseCors(appOptions.Cors.CommentService.AllowedOrigins.Any() ? "AllowSpecificOrigins" : "AllowAll");

    // WebSockets
    app.MapHub<WebSocketHub>("/ws");

    Log.Information("Starting Web Application...");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}

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

void ConfigureServicesAsync(IServiceCollection services, AppOptions appOptions)
{
    Log.Information($"Connection MSSQL: {appOptions.ConnectionStrings.DefaultConnection}");
    services.AddDbContext<ApplicationDbContext>(dbOptions =>
        dbOptions.UseSqlServer(appOptions.ConnectionStrings.DefaultConnection));

    Log.Information("RabbitMQ Host: {HostName}", appOptions.RabbitMq.HostName);
    Log.Information("RabbitMQ Port: {Port}", appOptions.RabbitMq.Port);
    Log.Information("RabbitMQ UserName: {UserName}", appOptions.RabbitMq.UserName);
    Log.Information("RabbitMQ Password: {Password}", appOptions.RabbitMq.Password);

    // Register RabbitMQ services
    services.AddSingleton<IConnectionFactory>(sp =>
    {
        return new ConnectionFactory
        {
            HostName = appOptions.RabbitMq.HostName,
            Port = appOptions.RabbitMq.Port,
            UserName = appOptions.RabbitMq.UserName,
            Password = appOptions.RabbitMq.Password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };
    });
    services.AddHostedService<RabbitMqConsumer>();

    // Services
    services.AddScoped<IFileAttachmentService, FileAttachmentService>();
    services.AddScoped<ISaveCommentService, SaveCommentService>();
    services.AddSignalR(options =>
    {
        options.KeepAliveInterval = TimeSpan.FromSeconds(30);
        options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
    })
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
    services.AddSingleton<WebSocketHub>();

    // Register repositories
    services.AddScoped<ICommentRepository, CommentRepository>();
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IFileAttachmentRepository, FileAttachmentRepository>();

    var corsOptions = appOptions.Cors;
    services.AddCors(options =>
    {
        if (corsOptions?.CommentService.AllowedOrigins?.Any() == true)
        {
            options.AddPolicy("AllowSpecificOrigins", builder =>
                builder.WithOrigins(corsOptions.CommentService.AllowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithExposedHeaders("Sec-WebSocket-Accept"));
        }
        else
        {
            Log.Warning("CORS is misconfigured: No allowed origins specified.");
            options.AddPolicy("AllowAll",
                builder =>
                builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("Sec-WebSocket-Accept"));
        }
    });
}
