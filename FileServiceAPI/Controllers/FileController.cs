using Microsoft.AspNetCore.Mvc;
using FileServiceAPI.Services.Interfaces;

namespace FileServiceAPI.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FileController : ControllerBase
    {
        private readonly IFileStorageService _fileStorageService;

        public FileController(IFileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Invalid file.");

            var result = await _fileStorageService.UploadFileAsync(file);
            return Ok(result);
        }
    }
}
