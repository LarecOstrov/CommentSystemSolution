namespace CaptchaServiceAPI.Services.Interfaces;

public interface ICaptchaService
{
    Task<(byte[], Guid)> GenerateCaptchaAsync();
}
