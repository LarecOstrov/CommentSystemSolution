using CommentSystem.Models;
using CommentSystem.Data;
using CommentSystem.Services.Interfaces;
using CommentSystem.Messaging.Interfaces;
using CommentSystem.Models.Inputs;
using CommentSystem.Helpers;

namespace CommentSystem.GraphQL
{
    public class Mutation
    {
        private readonly IRabbitMqProducer _rabbitMqProducer;
        private readonly CaptchaValidator _captchaValidator;

        public Mutation(IRabbitMqProducer rabbitMqProducer, CaptchaValidator captchaValidator)
        {
            _rabbitMqProducer = rabbitMqProducer;
            _captchaValidator = captchaValidator;
        }

        public async Task<string> AddComment(CommentDto input)
        {
            if (!await _captchaValidator.ValidateCaptchaAsync(input.Captcha))
            {
                throw new Exception("Invalid CAPTCHA");
            }

            _rabbitMqProducer.Publish("comments_queue", input);
            return "Comment is being processed";
        }
    }
}
