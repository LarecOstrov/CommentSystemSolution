namespace FileServiceAPI.Services.Interfaces;

internal interface IFileStorageService
{
    Task<string?> UploadFileAsync(IFormFile file);
    Task<bool> DeleteFileAsync(string fileUrl);
}
