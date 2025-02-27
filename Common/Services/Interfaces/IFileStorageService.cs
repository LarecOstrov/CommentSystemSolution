using Microsoft.AspNetCore.Http;

namespace Common.Services.Interfaces;

public interface IFileStorageService
{
    Task<string?> UploadFileAsync(IFormFile file);
    Task<bool> DeleteFileAsync(string fileUrl);
}
