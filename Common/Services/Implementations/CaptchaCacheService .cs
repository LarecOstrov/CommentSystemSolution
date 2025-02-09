using Common.Services.Interfaces;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;

namespace Common.Services.Implementations;

public class CaptchaCacheService : ICaptchaCacheService
{
    private readonly IDistributedCache _cache;

    public CaptchaCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<bool> ValidateCaptchaAsync(Guid captchaKey, string userInput)
    {
        if( captchaKey == Guid.Empty || string.IsNullOrWhiteSpace(userInput))
        {
            return false;
        }
        var expectedCaptcha = await _cache.GetStringAsync(captchaKey.ToString());
        if (expectedCaptcha is not null && expectedCaptcha.Equals(userInput, StringComparison.OrdinalIgnoreCase))
        {
            await RemoveCaptchaAsync(captchaKey);
            return true;
        }
        return false;
    }

    public async Task AddCaptchaAsync(Guid captchaKey, string captchaText, int lifeTimeMinutes)
    {
         
        await _cache.SetStringAsync(captchaKey.ToString(), captchaText, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(lifeTimeMinutes)
        });
    }

    public async Task RemoveCaptchaAsync(Guid captchaKey)
    {
        await _cache.RemoveAsync(captchaKey.ToString());
    }
}
