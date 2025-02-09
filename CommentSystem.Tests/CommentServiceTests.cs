using Common.Config;
using Common.Messaging.Interfaces;
using Common.Models;
using Common.Models.DTOs;
using Common.Models.Inputs;
using Common.Repositories.Interfaces;
using Common.Services.Implementations;
using Common.Services.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;

public class CommentServiceTests
{
    private readonly Mock<ICommentRepository> _commentRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly ICommentService _commentService;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<IRabbitMqProducer> _rabbitMqProducerMock;
    private readonly Mock<ICaptchaCacheService> _captchaCacheServiceMock;
    private readonly Mock<IFileStorageService> _fileStorageService;
    private readonly Mock<IFileAttachmentService> _fileAttachmentService;

    private readonly Mock<IValidator<CommentInput>> _validatorMock;
    private readonly Mock<IOptions<AppOptions>> _optionsMock;

    public CommentServiceTests()
    {
        _commentRepositoryMock = new Mock<ICommentRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _fileStorageService = new Mock<IFileStorageService>();
        _cacheMock = new Mock<IDistributedCache>();
        _rabbitMqProducerMock = new Mock<IRabbitMqProducer>();
        _captchaCacheServiceMock = new Mock<ICaptchaCacheService>();
        _fileAttachmentService = new Mock<IFileAttachmentService>();
        _validatorMock = new Mock<IValidator<CommentInput>>();
        _optionsMock = new Mock<IOptions<AppOptions>>();
        _commentService = new CommentService(_commentRepositoryMock.Object, _userRepositoryMock.Object, _cacheMock.Object,
            _rabbitMqProducerMock.Object, _captchaCacheServiceMock.Object, _fileStorageService.Object, _fileAttachmentService.Object, _validatorMock.Object, _optionsMock.Object);
    }

    [Fact]
    public async Task AddCommentAsync_Should_Add_Comment()
    {
        // Arrange
        var input = new CommentInput(
            UserName: "Test User",
            Email: "example@example.com",
            HomePage: "http://example.com",
            Text: "Test Comment",
            ParentId: null,
            CaptchaKey: Guid.NewGuid(),
            Captcha: "Test Captcha"
        );

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            Text = input.Text,
            User = new User
            {
                Id = Guid.NewGuid(),
                UserName = input.UserName,
                Email = input.Email,
                HomePage = input.HomePage
            }
        };

        _commentRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Comment>()))
            .ReturnsAsync(comment);

        // Act
        var commentDto = CommentDto.FromCommentInput(input);
        var result = await _commentService.AddCommentAsync(commentDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(input.Text, result.Text);
        Assert.Equal(input.UserName, result.User.UserName);
        _commentRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Comment>()), Times.Once);
    }

    [Fact]
    public async Task GetCommentsAsync_Should_Return_Comments()
    {
        // Arrange
        _commentRepositoryMock
            .Setup(repo => repo.GetAll())
            .Returns(new List<Comment>
            {
                new Comment { Id = Guid.NewGuid(), Text = "Test 1", UserId = Guid.NewGuid() },
                new Comment { Id = Guid.NewGuid(), Text = "Test 2", UserId = Guid.NewGuid() }
            }.AsQueryable());

        // Act
        var result = await Task.Run(() => _commentRepositoryMock.Object.GetAll().ToList());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        _commentRepositoryMock.Verify(repo => repo.GetAll().ToList(), Times.Once);
    }
}
