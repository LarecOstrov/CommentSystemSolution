namespace CaptchaServiceAPI.Config;

public class AppOptions
{
    public required RedisOptions Redis { get; init; }
    public CorsOptions Cors { get; set; } = new();
    public required int IpRateLimit { get; init; }
    public required CaptchaSettings CaptchaSettings { get; init; }

}

public class CorsOptions
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}

public class RedisOptions
{
    public required string Connection { get; init; }
    public required string InstanceName { get; init; }
}

public class CaptchaSettings
{
    public required int Length { get; init; }
    public required string Font { get; init; }
    public required float FontSize { get; init; }
    public required string FontColor { get; init; }
    public required string BackgroundColor { get; init; }
    public required int LifeTimeMinutes { get; init; }
    public required string EncryptionKey { get; init; }

}
