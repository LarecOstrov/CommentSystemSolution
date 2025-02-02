using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

namespace Common.Extensions;

public static class RateLimitingExtensions
{
    public static void ConfigureRateLimiting<TOptions>(
        this IServiceCollection services,
        TOptions appOptions,
        Func<TOptions, int> getUserRateLimit) where TOptions : class
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Get client IP address
                var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // Get rate limit for the user
                int permitLimit = getUserRateLimit(appOptions);

                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1)
                });
            });
        });
    }
}
