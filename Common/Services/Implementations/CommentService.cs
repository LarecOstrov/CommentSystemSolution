using Common.Config;
using Common.Helpers;
using Common.Messaging.Interfaces;
using Common.Models;
using Common.Models.DTOs;
using Common.Models.Inputs;
using Common.Repositories.Interfaces;
using Common.Services.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Json;

namespace Common.Services.Implementations;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IDistributedCache _cache;
    private readonly IRabbitMqProducer _rabbitMqProducer;
    private readonly ICaptchaCacheService _captchaCacheService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IValidator<CommentInput> _validator;
    private readonly AppOptions _options;
    private readonly Dictionary<string, string> _allowedMimeTypes;

    public CommentService(
        ICommentRepository commentRepository,
        IDistributedCache cache,
        IRabbitMqProducer rabbitMqProducer,
        ICaptchaCacheService captchaCacheService,
        IFileStorageService fileStorageService,
        IValidator<CommentInput> validator,
        IOptions<AppOptions> options)
    {
        _commentRepository = commentRepository;
        _cache = cache;
        _rabbitMqProducer = rabbitMqProducer;
        _captchaCacheService = captchaCacheService;
        _fileStorageService = fileStorageService;
        _validator = validator;
        _options = options.Value;

        _allowedMimeTypes = _options.FileUploadSettings.AllowedMimeTypes;
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

    public async Task ProcessingCommentAsync(CommentInput input, List<IFormFile>? fileAttachments)
    {
        List<string>? fileAttachmentUrls = new List<string>();
        try
        {
            if (!await _captchaCacheService.ValidateCaptchaAsync(input.CaptchaKey, input.Captcha))
            {
                throw new Exception("Invalid CAPTCHA");
            }

            ValidationResult validationResult = await _validator.ValidateAsync(input);
            if (!validationResult.IsValid)
            {
                throw new Exception(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
            }

            if (fileAttachments is not null)
            {
                foreach (var file in fileAttachments)
                {
                    if (!MimeTypeHelper.IsValidMimeType(file, _allowedMimeTypes))
                    {
                        Log.Warning($"File with unsupported extension: {file.Name}");
                        throw new Exception($"File with unsupported extension: {file.Name}");
                    }
                }

                foreach (var file in fileAttachments)
                {
                    var url = await _fileStorageService.UploadFileAsync(file);
                    if (url is not null)
                    {
                        fileAttachmentUrls.Add(url);
                    }
                }
            }


            var commentData = CommentDto.FromCommentInput(input, fileAttachmentUrls);

            await _rabbitMqProducer.Publish(commentData);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error while adding comment {input.Text}");
            throw;
        }
    }
}
