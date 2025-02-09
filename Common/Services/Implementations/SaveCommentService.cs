using Common.Models;
using Common.Models.DTOs;
using Common.Repositories.Interfaces;
using Common.Services.Interfaces;
using Serilog;

namespace Common.Services.Implementations;

public class SaveCommentService : ISaveCommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IFileAttachmentService _fileAttachmentService;

    public SaveCommentService(
        ICommentRepository commentRepository,
        IUserRepository userRepository,
        IFileAttachmentService fileAttachmentService
        )
    {
        _commentRepository = commentRepository;
        _userRepository = userRepository;
        _fileAttachmentService = fileAttachmentService;

    }


    public async Task<Comment> AddCommentAsync(CommentDto input)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(input.Email);

            if (user is null)
            {
                user = await _userRepository.AddAsync(new User
                {
                    UserName = input.UserName,
                    Email = input.Email,
                    HomePage = input.HomePage
                });
            }

            var comment = await _commentRepository.AddAsync(new Comment
            {
                Id = input.Id,
                User = user,
                ParentId = input.ParentId,
                Text = input.Text,
            });

            var fileAttachemnts = input.FileAttachmentUrls is not null && input.FileAttachmentUrls.Count > 0 ?
                input.FileAttachmentUrls.Select(url => new FileAttachment
                {
                    Id = Guid.NewGuid(),
                    CommentId = input.Id,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow,
                    Type = url.EndsWith(".txt") ? Enums.FileType.Text : Enums.FileType.Image,
                    Url = url
                }).ToList() : null;


            if (fileAttachemnts is not null && fileAttachemnts.Count > 0)
            {
                await _fileAttachmentService.AddManyFileAsync(fileAttachemnts);
            }

            comment.FileAttachments = fileAttachemnts?.ToList();

            return comment;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while adding comment");
            throw;
        }
    }

}
