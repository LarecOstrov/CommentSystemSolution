namespace Common.Config;

public class AppOptions
{
    public required ConnectionStringsOptions ConnectionStrings { get; init; }
    public required AzureBlobStorageOptions AzureBlobStorage { get; init; }
    public required FileUploadSettingsOptions FileUploadSettings { get; init; }
    public CorsOptions Cors { get; set; } = new CorsOptions();
    public required IpRateLimit IpRateLimit { get; init; }
    public required RedisOptions Redis { get; init; }
    public required RabbitMqOptions RabbitMq { get; init; }
    public required CaptchaSettings CaptchaSettings { get; init; }
}
public class IpRateLimit
{
    public required int CaptchaService { get; init; }
    public required int FileService { get; init; }
    public required int CommentService { get; init; }
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
public class RabbitMqOptions
{
    public required string HostName { get; set; }
    public required string UserName { get; set; }
    public required string Password { get; set; }
    public required int Port { get; set; }
    public required string QueueName { get; set; }
    public required string DeadQueueName { get; set; }
    public required string DeadExchangeName { get; set; }
}
public class ConnectionStringsOptions
{
    public required string DefaultConnection { get; init; }
}

public class CorsOptions
{
    public ServiceCors CaptchaService { get; init; } = new ServiceCors();
    public ServiceCors FileService { get; init; } = new ServiceCors();
    public ServiceCors CommentService { get; init; } = new ServiceCors();

}

public class ServiceCors
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}

public class AzureBlobStorageOptions
{
    public required string ConnectionString { get; init; }
    public required string ContainerName { get; init; }
}

public class FileUploadSettingsOptions
{
    public required List<string> AllowedImageExtensions { get; init; }
    public required List<string> AllowedTextExtension { get; init; }
    public required int MaxTextFileSize { get; init; }
    public required int MaxImageWidth { get; init; }
    public required int MaxImageHeight { get; init; }
    public required Dictionary<string, string> AllowedMimeTypes { get; init; }
}
public class RedisOptions
{
    public required string Connection { get; init; }
    public required string InstanceName { get; init; }
}
