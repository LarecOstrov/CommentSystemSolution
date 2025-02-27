namespace CaptchaServiceAPI.Models;

public class CaptchaValidationRequest
{
    public required Guid CaptchaKey { get; set; }
    public required string UserInput { get; set; }
}
