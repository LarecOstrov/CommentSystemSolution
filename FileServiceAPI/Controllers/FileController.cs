using Microsoft.AspNetCore.Mvc;
using Common.Services.Interfaces;
using Common.Enums;
using Common.Models;
using Common.Repositories.Interfaces;
using Serilog;
using FileServiceAPI.Services.Interfaces;

namespace FileServiceAPI.Controllers;

[ApiController]
[Route("api/files")]
public class FileController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ICaptchaCacheService _captchaCacheService;
    private readonly IFileAttachmentService _fileAttachmentService;
    private readonly ICommentRepository _commentRepository;

    public FileController(IFileStorageService fileStorageService, ICaptchaCacheService captchaCacheService,
        IFileAttachmentService fileAttachmentService, ICommentRepository commentRepository)
    {
        _fileStorageService = fileStorageService;
        _captchaCacheService = captchaCacheService;
        _fileAttachmentService = fileAttachmentService;
        _commentRepository = commentRepository;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] Guid captchaKey, [FromForm] string userInput)
    {
        string message = "";
        try
        {
            if (!await _captchaCacheService.ValidateCaptchaAsync(captchaKey, userInput))
            {
                return BadRequest("Invalid CAPTCHA");
            }                

            if (file is null || file.Length == 0)
            {
                message = "Invalid file";
            }
            else
            {
                var fileUrl = await _fileStorageService.UploadFileAsync(file);

                if (string.IsNullOrEmpty(fileUrl))
                {
                    message = "Error uploading file";
                }

                if (fileUrl is not null)
                {
                    if (await _fileAttachmentService.AddFileAsync(new FileAttachment
                    {
                        Url = fileUrl,
                        Type = file.ContentType.Contains("text") ? FileType.Text : FileType.Image,
                        CommentId = captchaKey
                    }) == null)
                    {
                        message = "Error saving Url";
                    }
                    else
                    {
                        return Ok(fileUrl);
                    }
                }
            }
            await _commentRepository.UpdateHasAttachmentAsync(captchaKey, false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while uploading file");
            message = ex.Message;
        }
        return BadRequest(message);
    }
}
