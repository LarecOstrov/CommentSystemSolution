using Microsoft.Extensions.Caching.Distributed;
using DNTCaptcha.Core;
using CaptchaServiceAPI.Services.Interfaces;
using Common.Config;
using Microsoft.Extensions.Options;
using Serilog;
using Common.Services.Interfaces;

namespace CaptchaServiceAPI.Services.Implementations;

internal class CaptchaService : ICaptchaService
{
    private readonly IDistributedCache _cache;
    private readonly ICaptchaCryptoProvider _captchaCryptoProvider;
    private readonly ICaptchaImageProvider _captchaImageProvider;
    private readonly ICaptchaCacheService _captchaCacheService;
    private readonly CaptchaSettings _captchaSettings;

    public CaptchaService(
        IDistributedCache cache,
        ICaptchaCryptoProvider captchaCryptoProvider,
        ICaptchaImageProvider captchaImageProvider,
        IOptions<AppOptions> options, ICaptchaCacheService captchaCacheService)
    {
        _cache = cache;
        _captchaCryptoProvider = captchaCryptoProvider;
        _captchaImageProvider = captchaImageProvider;
        _captchaSettings = options.Value.CaptchaSettings;
        _captchaCacheService = captchaCacheService;
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

                await _captchaCacheService.AddCaptchaAsync(captchaKey, captchaText, _captchaSettings.LifeTimeMinutes);

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
