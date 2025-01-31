namespace CommentSystem.Services.Interfaces
{
    public interface IFileServiceApiClient
    {
        Task<bool> DeleteFileAsync(string? fileUrl);
    }
}
