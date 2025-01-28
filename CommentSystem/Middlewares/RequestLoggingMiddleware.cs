using Microsoft.AspNetCore.Http;
using Serilog;
using System.Threading.Tasks;

namespace CommentSystem.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Получаем IP-адрес пользователя
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();

            // Получаем информацию о запросе
            var method = context.Request.Method;
            var path = context.Request.Path;
            var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.ToString() : "";

            // Логируем запрос
            Log.Information($"Incoming Request: {method} {path}{queryString} from IP {ipAddress}");

            // Передаём запрос дальше
            await _next(context);
        }
    }
}
