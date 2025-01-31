using CommentSystem.Messaging.Interfaces;
using CommentSystem.Models.DTOs;
using CommentSystem.Models.Inputs;
using CommentSystem.Services.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Serilog;
using System.Linq;

namespace CommentSystem.GraphQL
{
    public class Mutation
    {
        private readonly IRabbitMqProducer _rabbitMqProducer;
        private readonly IRemoteCaptchaService _remoteCaptchaService;
        private readonly IFileServiceApiClient _fileServiceApiClient;
        private readonly IValidator<AddCommentInput> _validator;

        public Mutation(IRabbitMqProducer rabbitMqProducer, IRemoteCaptchaService remoteCaptchaService, IFileServiceApiClient fileServiceApiClient, IValidator<AddCommentInput> validator)
        {
            _rabbitMqProducer = rabbitMqProducer;
            _remoteCaptchaService = remoteCaptchaService;
            _fileServiceApiClient = fileServiceApiClient;
            _validator = validator;
        }

        public async Task<string> AddComment(AddCommentInput input)
        {
            try
            {
                ValidationResult validationResult = await _validator.ValidateAsync(input);

                if (!validationResult.IsValid)
                {
                    throw new GraphQLException(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
                }

                if (!await _remoteCaptchaService.ValidateCaptchaAsync(input.CaptchaKey, input.Captcha))
                {
                    throw new GraphQLException("Invalid CAPTCHA");
                }

                var commentData = new CommentDto
                {
                    UserName = input.UserName,
                    Email = input.Email,
                    HomePage = input.HomePage,
                    Text = input.Text,
                    ImageUrl = input.ImageUrl,
                    TextUrl = input.TextUrl
                };

                await _rabbitMqProducer.Publish("comments_queue", commentData);
                return "Comment is being processed";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while adding comment");
                throw new GraphQLException("Internal server error");
            }
        }
    }
}
