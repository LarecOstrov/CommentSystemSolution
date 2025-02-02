namespace CaptchaServiceAPI.Services.Interfaces;

internal interface ICaptchaService
{
    Task<(byte[], Guid)> GenerateCaptchaAsync();
}
