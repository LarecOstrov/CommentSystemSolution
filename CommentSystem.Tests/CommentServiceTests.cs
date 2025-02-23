using Common.Config;
using Common.Messaging.Interfaces;
using Common.Models;
using Common.Models.DTOs;
using Common.Models.Inputs;
using Common.Repositories.Interfaces;
using Common.Services.Implementations;
using Common.Services.Interfaces;
using FluentValidation;
using HotChocolate.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;
using System.ComponentModel.DataAnnotations;
using System.Text;

public class CommentServiceTests
{
    private readonly Mock<ICommentRepository> _commentRepositoryMock;
    private readonly ICommentService _commentService;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<IRabbitMqProducer> _rabbitMqProducerMock;
    private readonly Mock<ICaptchaCacheService> _captchaCacheServiceMock;
    private readonly Mock<IFileStorageService> _fileStorageService;
    private readonly Mock<IValidator<CommentInput>> _validatorMock;
    private readonly Mock<IOptions<AppOptions>> _optionsMock;

    public CommentServiceTests()
    {
        _commentRepositoryMock = new Mock<ICommentRepository>();
        _fileStorageService = new Mock<IFileStorageService>();
        _cacheMock = new Mock<IDistributedCache>();
        _rabbitMqProducerMock = new Mock<IRabbitMqProducer>();
        _captchaCacheServiceMock = new Mock<ICaptchaCacheService>();
        _validatorMock = new Mock<IValidator<CommentInput>>();
        _optionsMock = new Mock<IOptions<AppOptions>>();        
        _commentService = new CommentService(
        _commentRepositoryMock.Object,
        _cacheMock.Object,
        _rabbitMqProducerMock.Object, 
        _captchaCacheServiceMock.Object, 
        _fileStorageService.Object,
        _validatorMock.Object,
        _optionsMock.Object);
    }

    [Fact]
    public async Task AddCommentAsync_Should_Add_Comment()
    {
        // Arrange
        var commentInput = new CommentInput(
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
            Text = commentInput.Text,
            User = new User
            {
                Id = Guid.NewGuid(),
                UserName = commentInput.UserName,
                Email = commentInput.Email,
                HomePage = commentInput.HomePage
            }
        };
        var testText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit";
        var testBytes = Encoding.UTF8.GetBytes(testText);
        var testStream = new MemoryStream(testBytes);
        var file = new FormFile(testStream, 0, testStream.Length, "Data", "test.txt");
        var files = new List<IFormFile> { file };

        _commentRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Comment>()))
            .ReturnsAsync(comment);

        // Act
        await _commentService.ProcessingCommentAsync(commentInput, files);

        // Assert
        Assert.NotNull(comment);
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
