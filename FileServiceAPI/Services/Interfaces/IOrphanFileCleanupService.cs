namespace FileServiceAPI.Services.Interfaces;
internal interface IOrphanFileCleanupService
{
    Task CleanupOrphanFilesAsync();
}
