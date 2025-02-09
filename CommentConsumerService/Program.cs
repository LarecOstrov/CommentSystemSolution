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

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting CommentConsumerService...");

    var builder = Host.CreateDefaultBuilder(args)
        .UseSerilog();

    // Load AppOptions from Common
    var appOptions = LoadAppOptionsHelper.LoadAppOptions(builder);

    builder.ConfigureServices((hostContext, services) =>
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
        services.AddSignalR();
        services.AddSingleton<WebSocketHub>();

        // Register repositories
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFileAttachmentRepository, FileAttachmentRepository>();
    });

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}
