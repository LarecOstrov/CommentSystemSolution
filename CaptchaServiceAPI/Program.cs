using CaptchaServiceAPI.Services.Implementations;
using CaptchaServiceAPI.Services.Interfaces;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to DI container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Captcha Service
builder.Services.AddSingleton<ICaptchaService, CaptchaService>();

// Enable CORS for frontend access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Configure Redis (Used for Captcha Validation & Storage)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "CaptchaService_";
});

var app = builder.Build();

// Middleware for logging incoming requests
app.Use(async (context, next) =>
{
    Log.Information($"Incoming request: {context.Request.Method} {context.Request.Path}");
    await next.Invoke();
    Log.Information($"Request completed: {context.Request.Method} {context.Request.Path} - {context.Response.StatusCode}");
});

// Enable Swagger **only in development mode**
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS
app.UseCors("AllowAll");

app.UseAuthorization();
app.MapControllers();

app.Run();
