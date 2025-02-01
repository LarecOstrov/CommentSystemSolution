using CommentSystem.Config;
using CommentSystem.Models;
using CommentSystem.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace CommentSystem.Helpers
{
    public class RemoteCaptchaService: IRemoteCaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _captchaServiceUrl;
        private readonly AppOptions _options;

        public RemoteCaptchaService(HttpClient httpClient, IOptions<AppOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _captchaServiceUrl = _options.CaptchaServiceUrl;
        }

        public async Task<bool> ValidateCaptchaAsync(string captchaKey, string userInput)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_captchaServiceUrl}/api/captcha/validate",
                new { CaptchaKey = captchaKey, UserInput = userInput });

            var result = await response.Content.ReadFromJsonAsync<CaptchaValidationResponse>();
            return result?.IsValid ?? false;
        }
    }
}
