namespace FileServiceAPI.Services.Interfaces
{
    public interface IFileStorageService
    {
        Task<string?> UploadFileAsync(IFormFile file);
        Task<bool> DeleteFileAsync(string fileUrl);
    }
}
