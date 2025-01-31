using System;
using System.IO;
using System.Threading.Tasks;
using CaptchaGen;
using CaptchaServiceAPI.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace CaptchaServiceAPI.Services.Implementations
{
    public class CaptchaService : ICaptchaService
    {
        private readonly IDistributedCache _cache;

        public CaptchaService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<(byte[], string)> GenerateCaptchaAsync()
        {
            var captchaText = CaptchaCodeFactory.GenerateCaptchaCode(6);

            using var imageStream = ImageFactory.GenerateImage(captchaText, 150, 50, 30);
            imageStream.Position = 0;
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            byte[] imageBytes = memoryStream.ToArray();

            string captchaKey = Guid.NewGuid().ToString();
            await _cache.SetStringAsync(captchaKey, captchaText, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return (imageBytes, captchaKey);
        }

        public async Task<bool> ValidateCaptchaAsync(string captchaKey, string userInput)
        {
            var expectedCaptcha = await _cache.GetStringAsync(captchaKey);
            return expectedCaptcha != null && expectedCaptcha.Equals(userInput, StringComparison.OrdinalIgnoreCase);
        }
    }
}
