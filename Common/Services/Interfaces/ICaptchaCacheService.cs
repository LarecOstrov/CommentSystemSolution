﻿namespace Common.Services.Interfaces;

public interface ICaptchaCacheService
{
    Task<bool> ValidateCaptchaAsync(Guid captchaKey, string userInput);
    Task RemoveCaptchaAsync(Guid captchaKey);
    Task AddCaptchaAsync(Guid captchaKey, string captchaText, int lifeTimeMinutes);
}
