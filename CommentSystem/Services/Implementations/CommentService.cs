using CommentSystem.Models;
using CommentSystem.Repositories.Interfaces;
using CommentSystem.Services.Interfaces;
using CommentSystem.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using CommentSystem.Models.Inputs;

namespace CommentSystem.Services.Implementations
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IDistributedCache _cache;
        private readonly CaptchaValidator _captchaValidator;

        public CommentService(ICommentRepository commentRepository, IDistributedCache cache, CaptchaValidator captchaValidator)
        {
            _commentRepository = commentRepository;

            _cache = cache;
            _captchaValidator = captchaValidator;
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
            var comment = new Comment
            {
                User = new User
                {
                    UserName = input.UserName,
                    Email = input.Email,
                    HomePage = input.HomePage
                },
                Text = input.Text
            };

            await _commentRepository.AddAsync(comment);
        }
    }
}