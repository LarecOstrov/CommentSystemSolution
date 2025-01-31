using CommentSystem.Models;
using CommentSystem.Services.Interfaces;

namespace CommentSystem.Helpers
{
    public class RemoteCaptchaService: IRemoteCaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _captchaServiceUrl;

        public RemoteCaptchaService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _captchaServiceUrl = configuration["CaptchaServiceUrl"] ?? "http://localhost:5002";
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
