namespace CaptchaServiceAPI.Models
{
    public class CaptchaValidationRequest
    {
        public string CaptchaKey { get; set; } = string.Empty;
        public string UserInput { get; set; } = string.Empty;
    }
}
