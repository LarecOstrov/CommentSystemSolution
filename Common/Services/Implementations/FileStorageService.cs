using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Common.Config;
using Common.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

public class FileStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly List<string> _allowedImageExtensions;
    private readonly List<string> _allowedTextExtension;
    private readonly int _maxTextFileSize;
    private readonly int _maxImageWidth;
    private readonly int _maxImageHeight;
    private readonly AppOptions _options;
    private readonly Dictionary<string, string> _allowedMimeTypes;

    public FileStorageService(IOptions<AppOptions> options)
    {
        _options = options.Value;
        _blobServiceClient = new BlobServiceClient(_options.AzureBlobStorage.ConnectionString);
        _containerName = _options.AzureBlobStorage.ContainerName;
        _allowedImageExtensions = _options.FileUploadSettings.AllowedImageExtensions;
        _allowedTextExtension = _options.FileUploadSettings.AllowedTextExtension;
        _maxTextFileSize = _options.FileUploadSettings.MaxTextFileSize;
        _maxImageWidth = _options.FileUploadSettings.MaxImageWidth;
        _maxImageHeight = _options.FileUploadSettings.MaxImageHeight;
        _allowedMimeTypes = _options.FileUploadSettings.AllowedMimeTypes;
    }

    public async Task<string?> UploadFileAsync(IFormFile file)
    {
        try
        {
            if (file == null || file.OpenReadStream() == null)
            {
                Log.Warning("Received null file");
                throw new ArgumentNullException(nameof(file));
            }

            using var fileStream = file.OpenReadStream();
            string fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (_allowedImageExtensions.Contains(fileExtension))
            {
                return await ProcessImageUploadAsync(fileStream, file.FileName);
            }
            else if (_allowedTextExtension.Contains(fileExtension))
            {
                return await ProcessTextFileUploadAsync(fileStream, file.FileName);
            }

            throw new ArgumentException($"File type {fileExtension} is not supported");
        }
        catch (Exception ex)
        {
            Log.Error($"Error uploading file: {ex.Message}");
            throw;
        }
    }

    private async Task<string?> ProcessImageUploadAsync(Stream fileStream, string fileName)
    {
        using var image = await Image.LoadAsync(fileStream);

        if (image.Width > _maxImageWidth || image.Height > _maxImageHeight)
        {
            Log.Information($"Resizing image {fileName}...");
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(_maxImageWidth, _maxImageHeight)
            }));
        }

        using var stream = new MemoryStream();
        await image.SaveAsync(stream, new JpegEncoder());
        stream.Position = 0;

        return await UploadToBlobWithRetryAsync(stream, fileName);
    }

    private async Task<string?> ProcessTextFileUploadAsync(Stream fileStream, string fileName)
    {
        if (fileStream.Length > _maxTextFileSize)
        {
            Log.Warning($"Text file {fileName} exceeds the size limit of {_maxTextFileSize} bytes");
            throw new ArgumentException($"Text file {fileName} exceeds the size limit of {_maxTextFileSize} bytes");
        }

        return await UploadToBlobWithRetryAsync(fileStream, fileName);
    }


    private async Task<string?> UploadToBlobWithRetryAsync(Stream fileStream, string fileName, int maxRetries = 3)
    {
        var blobContainer = _blobServiceClient.GetBlobContainerClient(_containerName);
        var safeFileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(fileName.Replace(" ", "_"))}{Path.GetExtension(fileName).ToLower()}";
        var blobClient = blobContainer.GetBlobClient(safeFileName);

        var extension = Path.GetExtension(fileName).ToLower();
        var isImage = _allowedImageExtensions.Contains(extension);
        var contentType = isImage ? _allowedMimeTypes[extension] : "text/plain";
        var contentDisposition = isImage ? "inline" : "attachment";

        int retryCount = 0;
        while (retryCount < maxRetries)
        {
            try
            {
                var options = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType,
                        ContentDisposition = contentDisposition
                    }
                };

                await blobClient.UploadAsync(fileStream, options);
                Log.Information($"File uploaded successfully: {blobClient.Uri}");
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                retryCount++;
                Log.Warning($"Retry {retryCount}/{maxRetries} failed for file {fileName}: {ex.Message}");
                await Task.Delay(1000);
            }
        }

        Log.Error($"File {fileName} failed to upload after {maxRetries} attempts.");
        return null;
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            Uri uri = new Uri(fileUrl);
            string blobName = uri.Segments.Last();
            var blobContainer = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = blobContainer.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync();
            Log.Information($"File deleted successfully: {fileUrl}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Error deleting file {fileUrl}: {ex.Message}");
            return false;
        }
    }
}
