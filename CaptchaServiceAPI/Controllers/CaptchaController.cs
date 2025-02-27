using CaptchaServiceAPI.Models;
using CaptchaServiceAPI.Services.Interfaces;
using Common.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace CaptchaServiceAPI.Controllers;

[Route("api/captcha")]
[ApiController]
public class CaptchaController : ControllerBase
{
    private readonly ICaptchaService _captchaService;
    private readonly ICaptchaCacheService _captchaCacheService;


    public CaptchaController(ICaptchaService captchaService, ICaptchaCacheService captchaCacheService)
    {
        _captchaService = captchaService;
        _captchaCacheService = captchaCacheService;
    }

    [HttpGet("generate")]
    public async Task<IActionResult> GenerateCaptcha()
    {
        try
        {
            var (imageBytes, captchaKey) = await _captchaService.GenerateCaptchaAsync();
            var base64Image = Convert.ToBase64String(imageBytes);

            return Ok(new { image = $"data:image/png;base64,{base64Image}", captchaKey });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while generating captcha");
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateCaptcha([FromBody] CaptchaValidationRequest request)
    {
        try
        {
            if (await _captchaCacheService.ValidateCaptchaAsync(request.CaptchaKey, request.UserInput))
            {
                return Ok(new { isValid = true });
            }
            return BadRequest((new { isValid = false }));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while validating captcha");
            return BadRequest(new { isValid = true });
        }
    }
}
