namespace FileServiceAPI.Config;

public class AppOptions
{
    public required ConnectionStringsOptions ConnectionStrings { get; init; }
    public required AzureBlobStorageOptions AzureBlobStorage { get; init; }
    public required FileUploadSettingsOptions FileUploadSettings { get; init; }
    public CorsOptions Cors { get; set; } = new();
    public required int IpRateLimit { get; init; }
}

public class ConnectionStringsOptions
{
    public required string DefaultConnection { get; init; }
}

public class CorsOptions
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
    public required string AllowedTextExtension { get; init; }
    public required int MaxTextFileSize { get; init; }
    public required int MaxImageWidth { get; init; }
    public required int MaxImageHeight { get; init; }
    public required Dictionary<string, string> AllowedMimeTypes { get; init; }
}
