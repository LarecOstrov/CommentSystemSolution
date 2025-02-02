using Common.Models;
using Common.Repositories.Interfaces;
using FileServiceAPI.Services.Interfaces;
using Serilog;

namespace FileServiceAPI.Services.Implementations;
internal class OrphanFileCleanupService : IOrphanFileCleanupService
{
    private readonly IFileAttachmentRepository _fileRepository;
    private readonly IFileStorageService _fileStorageService;

    public OrphanFileCleanupService(
        IFileAttachmentRepository fileRepository,
        IFileStorageService fileStorageService)
    {
        _fileRepository = fileRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task CleanupOrphanFilesAsync()
    {
        Log.Information("Starting orphan file cleanup...");

        var oldFiles = await _fileRepository.GetOrphanFilesAsync(TimeSpan.FromHours(1));
        var successDeleted = new List<FileAttachment>();
        foreach (var file in oldFiles)
        {
            try
            {
                Log.Information($"Deleting orphan file: {file.Url}");
                if (await _fileStorageService.DeleteFileAsync(file.Url))
                {
                    successDeleted.Add(file);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to delete orphan file: {file.Url}");
            }
        }

        await _fileRepository.DeleteManyAsync(successDeleted);

        Log.Information("Orphan file cleanup completed.");
    }
}
