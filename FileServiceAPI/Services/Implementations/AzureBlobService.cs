﻿using System.Drawing;
using System.Drawing.Imaging;
using Azure.Storage.Blobs;
using FileServiceAPI.Services.Interfaces;
using FileServiceAPI.Config;
using Serilog;
using Microsoft.Extensions.Options;
using Azure.Storage.Blobs.Models;

namespace FileServiceAPI.Services
{
    public class AzureBlobService : IFileStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        private readonly List<string> _allowedImageExtensions;
        private readonly string _allowedTextExtension;
        private readonly int _maxTextFileSize;
        private readonly int _maxImageWidth;
        private readonly int _maxImageHeight;
        private readonly AppOptions _options;
        private readonly Dictionary<string, string> _allowedMimeTypes;


        public AzureBlobService(IOptions<AppOptions> options)
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
            if (!IsValidMimeType(file))
            {
                Log.Warning($"File with wrong MIME-type: {file.FileName}, {file.ContentType}");
                return null;
            }

            string fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (_allowedImageExtensions.Contains(fileExtension))
            {
                return await ProcessImageUploadAsync(file);
            }
            else if (fileExtension == _allowedTextExtension)
            {
                return await ProcessTextFileUploadAsync(file);
            }

            return null;
        }

        private bool IsValidMimeType(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLower();
            return _allowedMimeTypes.ContainsKey(ext) && file.ContentType == _allowedMimeTypes[ext];
        }

        private async Task<string?> ProcessImageUploadAsync(IFormFile file)
        {
            try
            {
                using var image = Image.FromStream(file.OpenReadStream());

                if (image.Width > _maxImageWidth || image.Height > _maxImageHeight)
                {
                    Log.Information($"Resizing image {file.FileName}...");
                    using var resizedImage = ResizeImage(image, _maxImageWidth, _maxImageHeight);

                    using var stream = new MemoryStream();
                    resizedImage.Save(stream, ImageFormat.Jpeg);
                    stream.Position = 0;

                    return await UploadToBlobWithRetryAsync(stream, file.FileName);
                }

                return await UploadToBlobWithRetryAsync(file.OpenReadStream(), file.FileName);
            }
            catch (Exception ex)
            {
                Log.Error($"Error processing image {file.FileName}: {ex.Message}");
                return null;
            }
        }

        private async Task<string?> ProcessTextFileUploadAsync(IFormFile file)
        {
            if (file.Length > _maxTextFileSize)
            {
                Log.Warning($"Text file {file.FileName} exceeds the size limit of 100 KB");
                return null;
            }

            return await UploadToBlobWithRetryAsync(file.OpenReadStream(), file.FileName);
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


        private static Image ResizeImage(Image image, int maxWidth, int maxHeight)
        {
            double ratioX = (double)maxWidth / image.Width;
            double ratioY = (double)maxHeight / image.Height;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = (int)(image.Width * ratio);
            int newHeight = (int)(image.Height * ratio);

            var resized = new Bitmap(newWidth, newHeight);
            using var graphics = Graphics.FromImage(resized);
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.DrawImage(image, 0, 0, newWidth, newHeight);

            return resized;
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
                Log.Information($"🗑 File deleted successfully: {fileUrl}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"❌ Error deleting file {fileUrl}: {ex.Message}");
                return false;
            }
        }
    }
}
