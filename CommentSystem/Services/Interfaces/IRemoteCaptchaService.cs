namespace CommentSystem.Services.Interfaces
{
    public interface IRemoteCaptchaService
    {
        Task<bool> ValidateCaptchaAsync(string captchaKey, string userInput);
    }
}
