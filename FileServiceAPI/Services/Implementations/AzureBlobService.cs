using System.Drawing;
using System.Drawing.Imaging;
using Azure.Storage.Blobs;
using CommentSystem.Services.Interfaces;
using Serilog;

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

        public AzureBlobService(IConfiguration configuration)
        {
            _blobServiceClient = new BlobServiceClient(configuration["AzureBlobStorage:ConnectionString"]);
            _containerName = configuration["AzureBlobStorage:ContainerName"];
            _allowedImageExtensions = configuration.GetSection("FileUploadSettings:AllowedImageExtensions").Get<List<string>>() ?? new List<string>{ ".jpg", ".jpeg", ".png", ".gif" };
            _allowedTextExtension = configuration["FileUploadSettings:AllowedTextExtension"] ?? ".txt";
            _maxTextFileSize = configuration.GetValue<int?>("FileUploadSettings:MaxTextFileSize") ?? 102400;
            _maxImageWidth = configuration.GetValue<int?>("FileUploadSettings:MaxImageWidth") ?? 320;
            _maxImageHeight = configuration.GetValue<int?>("FileUploadSettings:MaxImageHeight") ?? 240;
        }

        public async Task<string?> UploadFileAsync(IFormFile file)
        {
            try
            {
                string fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!_allowedImageExtensions.Contains(fileExtension) && fileExtension != _allowedTextExtension)
                {
                    Log.Warning($"Wrong extentions {file.FileName}");
                    return null;
                }

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
            catch (Exception ex)
            {
                Log.Error($"Error uploading file {file.FileName}: {ex.Message}");
                return null;
            }
        }

        private async Task<string?> ProcessImageUploadAsync(IFormFile file)
        {
            using var image = Image.FromStream(file.OpenReadStream()); //Added using for linux

            if (image.Width > _maxImageWidth || image.Height > _maxImageHeight)
            {
                Log.Information($"{file.FileName} resize...");
                using var resizedImage = ResizeImage(image, _maxImageWidth, _maxImageHeight);

                using var stream = new MemoryStream();
                resizedImage.Save(stream, ImageFormat.Jpeg); // Или другой формат (PNG, GIF)
                stream.Position = 0;

                return await UploadToBlobAsync(stream, file.FileName);
            }

            return await UploadToBlobAsync(file.OpenReadStream(), file.FileName);
        }


        private async Task<string?> ProcessTextFileUploadAsync(IFormFile file)
        {
            if (file.Length > _maxTextFileSize)
            {
                Log.Warning($"Text {file.FileName} file limit 100 KB");
                return null;
            }

            return await UploadToBlobAsync(file.OpenReadStream(), file.FileName);
        }

        private async Task<string?> UploadToBlobAsync(Stream fileStream, string fileName)
        {
            try
            {
                var blobContainer = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = blobContainer.GetBlobClient($"{Guid.NewGuid()}_{fileName}");

                await blobClient.UploadAsync(fileStream, true);
                Log.Information($"File {fileName} uploaded: {blobClient.Uri}");
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                Log.Error($"Error file uploading {fileName}: {ex.Message}");
                return null;
            }
        }

        private static Image ResizeImage(Image image, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

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
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Error file removing {fileUrl}: {ex.Message}");
                return false;
            }
        }
    }
}
