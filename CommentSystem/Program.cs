using Microsoft.EntityFrameworkCore;
using CommentSystem.Data;
using CommentSystem.Services.Interfaces;
using CommentSystem.Services.Implementations;
using CommentSystem.Repositories.Interfaces;
using CommentSystem.Repositories.Implementations;
using CommentSystem.GraphQL;
using Serilog;
using CommentSystem.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using Prometheus;
using CommentSystem.GraphQL.Inputs;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services
    .AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddFiltering()
    .AddSorting()
    .AddInstrumentation();
builder.Services.AddValidation();
builder.Services.AddSingleton<IValidator<AddCommentInput>, AddCommentInputValidator>();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "CommentSystem_";
});


var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseMiddleware<RequestLoggingMiddleware>();

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
dbContext.Database.Migrate();

// ?????????? ?????? HTTP
app.UseHttpMetrics();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthorization();
app.MapGraphQL(); 
app.MapMetrics(); // Prometheus
//app.MapControllers();

app.Run();
