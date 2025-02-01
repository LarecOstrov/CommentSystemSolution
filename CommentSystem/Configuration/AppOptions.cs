using System.Collections.Generic;

namespace CommentSystem.Config
{

    public class AppOptions
    {
        public required ConnectionStringsOptions ConnectionStrings { get; init; }
        public required RedisOptions Redis { get; init; }
        public required RabbitMqOptions RabbitMq { get; init; }
        public required string CaptchaServiceUrl { get; init; }
        public required int IpRateLimit { get; init; }
    }

    public class ConnectionStringsOptions
    {
        public required string DefaultConnection { get; init; }
    }

    public class RedisOptions
    {
        public required string Connection { get; init; }
        public required string InstanceName { get; init; }
    }

    
    public class RabbitMqOptions
    {
        public required string HostName { get; init; }
        public required string UserName { get; init; }
        public required string Password { get; init; }
        public required int Port { get; init; }
        public required string QueueName { get; init; }
    }    
}
