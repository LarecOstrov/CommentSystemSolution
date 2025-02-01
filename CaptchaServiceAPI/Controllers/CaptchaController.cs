using Microsoft.AspNetCore.Mvc;
using CaptchaServiceAPI.Models;
using CaptchaServiceAPI.Services.Implementations;
using Serilog;
using CaptchaServiceAPI.Services.Interfaces;

namespace CaptchaServiceAPI.Controllers
{
    [Route("api/captcha")]
    [ApiController]
    public class CaptchaController : ControllerBase
    {
        private readonly ICaptchaService _captchaService;

        public CaptchaController(ICaptchaService captchaService)
        {
            _captchaService = captchaService;
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
                var isValid = await _captchaService.ValidateCaptchaAsync(request.CaptchaKey, request.UserInput);
                return Ok(new { isValid });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while validating captcha");
                return BadRequest(new { isValid = true });
            }
        }
    }    
}
