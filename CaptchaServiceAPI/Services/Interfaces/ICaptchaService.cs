namespace CaptchaServiceAPI.Services.Interfaces
{
    public interface ICaptchaService
    {
        Task<(byte[], string)> GenerateCaptchaAsync();
        Task<bool> ValidateCaptchaAsync(string captchaKey, string userInput);
    }
}
