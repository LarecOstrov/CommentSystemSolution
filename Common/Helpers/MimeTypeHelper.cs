using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Serilog;


namespace Common.Helpers
{
    public static class MimeTypeHelper
    {
        public static bool IsValidMimeType(IFormFile file, Dictionary<string, string> allowedMimeTypes)
        {
            if (file == null || file.Length == 0)
            {
                throw new Exception($"File is empty: {file?.FileName}");
            }

            var ext = Path.GetExtension(file.FileName)?.ToLower();

            if (string.IsNullOrEmpty(ext) || !allowedMimeTypes.ContainsKey(ext))
            {
                return false;
            }

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(file.FileName, out var detectedMime))
            {
                Log.Warning("Could not determine MIME type from extension.");
                return false;
            }

            Log.Information($"Detected MIME: {detectedMime}");

            return allowedMimeTypes.ContainsKey(ext) && detectedMime == allowedMimeTypes[ext];
        }
    }
}
