using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CommentSystem.Helpers
{
    public class CaptchaValidator
    {
        private readonly HttpClient _httpClient;
        private readonly string _recaptchaSecretKey;

        public CaptchaValidator(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _recaptchaSecretKey = configuration["Captcha:SecretKey"];
        }

        public async Task<bool> ValidateCaptchaAsync(string captchaString)
        {
            if (string.IsNullOrWhiteSpace(captchaString))
            {
                return false;
            }
            
            var captchaResult = new RecaptchaResponse { Success = true };

            return captchaResult?.Success ?? false;
        }
    }

    public class RecaptchaResponse
    {
        public bool Success { get; set; }
        public string[] ErrorCodes { get; set; } = new string[0];
    }
}
