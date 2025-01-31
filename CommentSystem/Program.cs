using CommentSystem.Data;
using CommentSystem.GraphQL;
using CommentSystem.Messaging.Consumers;
using CommentSystem.Messaging.Interfaces;
using CommentSystem.Messaging.Producers;
using CommentSystem.Middleware;
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

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// MSSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// RabbitMQ
var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");
var factory = new ConnectionFactory
{
    HostName = rabbitMqConfig["HostName"],
    UserName = rabbitMqConfig["UserName"],
    Password = rabbitMqConfig["Password"],
    Port = int.Parse(rabbitMqConfig["Port"])
};

// Rabbit connection
var rabbitConnection = await factory.CreateConnectionAsync();
builder.Services.AddSingleton<IConnection>(rabbitConnection);
builder.Services.AddSingleton<IRabbitMqProducer, RabbitMqProducer>();
builder.Services.AddHostedService<RabbitMqConsumer>();

// Repositories
builder.Services.AddScoped<ICommentRepository, CommentRepository>();

//Services
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IRemoteCaptchaService, RemoteCaptchaService>();
builder.Services.AddHttpClient<IFileServiceApiClient, FileServiceApiClient>();

// Helpers

// GraphQL
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddFiltering()
    .AddSorting()
    .AddInstrumentation();
builder.Services.AddValidation();
builder.Services.AddScoped<IValidator<AddCommentInput>, AddCommentInputValidator>();

// Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "CommentSystem_";
});

builder.Services.AddSignalR();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseMiddleware<RequestLoggingMiddleware>();

// Auto-Migration DB for DEV
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpMetrics();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapHub<WebSocketHub>("/ws"); //WebSocket
app.MapGraphQL();
app.MapMetrics(); // Prometheus

app.Run();
