using Microsoft.Extensions.Caching.Distributed;
using DNTCaptcha.Core;
using CaptchaServiceAPI.Services.Interfaces;
using CaptchaServiceAPI.Config;
using Microsoft.Extensions.Options;
using Serilog;

namespace CaptchaServiceAPI.Services.Implementations;

internal class CaptchaService : ICaptchaService
{
    private readonly IDistributedCache _cache;
    private readonly ICaptchaCryptoProvider _captchaCryptoProvider;
    private readonly ICaptchaImageProvider _captchaImageProvider;
    private readonly CaptchaSettings _captchaSettings;

    public CaptchaService(
        IDistributedCache cache,
        ICaptchaCryptoProvider captchaCryptoProvider,
        ICaptchaImageProvider captchaImageProvider,
        IOptions<AppOptions> options)
    {
        _cache = cache;
        _captchaCryptoProvider = captchaCryptoProvider;
        _captchaImageProvider = captchaImageProvider;
        _captchaSettings = options.Value.CaptchaSettings;
    }

    public async Task<(byte[], Guid)> GenerateCaptchaAsync()
    {
        const int maxRetries = 3;
        int attempt = 0;

        while (attempt < 3)
        {
            try
            {
                // Random captcha text
                var captchaText = _captchaCryptoProvider.Decrypt(
                    _captchaCryptoProvider.Encrypt(Guid.NewGuid().ToString("N").Substring(0, _captchaSettings.Length))
                );

                if (string.IsNullOrWhiteSpace(captchaText))
                {
                    throw new InvalidOperationException("Captcha text cannot be null or empty.");
                }

                // Generate captcha image
                var captchaImageBytes = _captchaImageProvider.DrawCaptcha(
                    captchaText,
                    _captchaSettings.FontColor,
                    _captchaSettings.BackgroundColor,
                    _captchaSettings.FontSize,
                    _captchaSettings.Font
                );

                // Generate captcha key
                Guid captchaKey = Guid.NewGuid();

                await _cache.SetStringAsync(captchaKey.ToString(), captchaText, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_captchaSettings.LifeTimeMinutes)
                });

                return (captchaImageBytes, captchaKey);
            }
            catch (Exception ex)
            {
                attempt++;
                Log.Warning($"Captcha generation failed (Attempt {attempt}/{maxRetries}): {ex.Message}");

                if (attempt >= maxRetries)
                {
                    Log.Error($"Captcha generation failed after {attempt} attempts.");
                    throw;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt));
            }
        }

        throw new InvalidOperationException("Unexpected error in captcha generation.");
    }
}
