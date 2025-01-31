using CommentSystem.Services.Interfaces;
using Serilog;

namespace CommentSystem.Services.Implementations
{
    public class FileServiceApiClient : IFileServiceApiClient
    {
        private readonly HttpClient _httpClient;

        public FileServiceApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> DeleteFileAsync(string? fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                {
                    return false;
                }
            
                var response = await _httpClient.DeleteAsync($"api/files/delete?fileUrl={fileUrl}");

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"Ошибка удаления файла {fileUrl}: {response.StatusCode}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка при удалении файла {fileUrl}: {ex.Message}");
                return false;
            }
        }
    }
}
