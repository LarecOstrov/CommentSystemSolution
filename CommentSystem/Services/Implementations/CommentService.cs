using CommentSystem.Config;
using Common.Messaging.Interfaces;
using Common.Models;
using Common.Models.DTOs;
using Common.Models.Inputs;
using Common.Repositories.Interfaces;
using Common.Services.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Json;

namespace Common.Services.Implementations;

internal class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IDistributedCache _cache;
    private readonly IRabbitMqProducer _rabbitMqProducer;
    private readonly ICaptchaCacheService _captchaCacheService;
    private readonly IValidator<AddCommentInput> _validator;
    private readonly AppOptions _options;
    private readonly string _queueName;

    public CommentService(
        ICommentRepository commentRepository,
        IDistributedCache cache,
        IRabbitMqProducer rabbitMqProducer,
        ICaptchaCacheService captchaCacheService,
        IValidator<AddCommentInput> validator,
        IOptions<AppOptions> options)
    {
        _commentRepository = commentRepository;
        _cache = cache;
        _rabbitMqProducer = rabbitMqProducer;
        _captchaCacheService = captchaCacheService;
        _validator = validator;
        _options = options.Value;
        _queueName = _options.RabbitMq.QueueName;
    }

    public async Task<List<Comment>> GetAllCommentsWithSortingAndPaginationAsync(string? sortBy, bool descending, int page, int pageSize)
    {
        var cacheKey = $"comments_{sortBy}_{descending}_{page}_{pageSize}";
        var cachedComments = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedComments))
        {
            return JsonSerializer.Deserialize<List<Comment>>(cachedComments)!;
        }

        var comments = _commentRepository.GetAll();

        // Sorting
        comments = sortBy?.ToLower() switch
        {
            "username" => descending ? comments.OrderByDescending(c => c.User.UserName) : comments.OrderBy(c => c.User.UserName),
            "email" => descending ? comments.OrderByDescending(c => c.User.Email) : comments.OrderBy(c => c.User.Email),
            _ => descending ? comments.OrderByDescending(c => c.CreatedAt) : comments.OrderBy(c => c.CreatedAt), // Default sorting
        };

        // Pagination
        comments = comments.Skip((page - 1) * pageSize).Take(pageSize);

        var commentList = await comments.ToListAsync();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(commentList), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        return commentList;
    }

    public async Task AddCommentAsync(CommentDto input)
    {
        try
        {
            var comment = new Comment
            {
                Id = input.Id,
                User = new User
                {
                    UserName = input.UserName,
                    Email = input.Email,
                    HomePage = input.HomePage
                },
                Text = input.Text,
                HasAttachment = input.HasAttachment
            };

            await _commentRepository.AddAsync(comment);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while adding comment");
        }
    }

    public async Task PublishCommentAsync(AddCommentInput input)
    {
        ValidationResult validationResult = await _validator.ValidateAsync(input);

        if (!validationResult.IsValid)
        {
            throw new Exception(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
        }

        if (!await _captchaCacheService.ValidateCaptchaAsync(input.CaptchaKey, input.Captcha))
        {
            throw new Exception("Invalid CAPTCHA");
        }

        var commentData = CommentDto.FromAddCommentInput(input);
        await _rabbitMqProducer.Publish(_queueName, commentData);
    }

    public async Task UpdateHasAttachmentAsync(Guid id, bool hasAttachment = false)
    {
        try
        {
            await _commentRepository.UpdateHasAttachmentAsync(id, hasAttachment);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while adding comment");
        }
    }
}
