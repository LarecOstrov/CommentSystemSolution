using Common.Models.Inputs;
using Common.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CommentSystem.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddComment(
        [FromForm] CommentInput input,
        [FromForm] List<IFormFile>? fileAttachments)
    {
        try
        {  
            await _commentService.ProcessingCommentAsync(input, fileAttachments);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest($"Error while processing comment: {ex.Message}");
        }
    }
}
