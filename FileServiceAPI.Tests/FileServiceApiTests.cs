using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using FileServiceAPI.Controllers;
using FileServiceAPI.Services.Interfaces;
using Common.Services.Interfaces;
using Common.Repositories.Interfaces;
using Common.Models;
using Common.Enums;

public class FileServiceApiTests
{
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<ICaptchaCacheService> _captchaCacheServiceMock;
    private readonly Mock<IFileAttachmentService> _fileAttachmentServiceMock;
    private readonly Mock<ICommentRepository> _commentRepositoryMock;
    private readonly FileController _controller;

    public FileServiceApiTests()
    {
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _captchaCacheServiceMock = new Mock<ICaptchaCacheService>();
        _fileAttachmentServiceMock = new Mock<IFileAttachmentService>();
        _commentRepositoryMock = new Mock<ICommentRepository>();

        _controller = new FileController(
            _fileStorageServiceMock.Object,
            _captchaCacheServiceMock.Object,
            _fileAttachmentServiceMock.Object,
            _commentRepositoryMock.Object
        );
    }

    [Fact]
    public async Task UploadFile_ShouldReturn_BadRequest_WhenCaptchaIsInvalid()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var captchaKey = Guid.NewGuid();
        _captchaCacheServiceMock
            .Setup(s => s.ValidateCaptchaAsync(captchaKey, "invalid_captcha"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UploadFile(fileMock.Object, captchaKey, "invalid_captcha");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid CAPTCHA", badRequestResult.Value);
    }

    [Fact]
    public async Task UploadFile_ShouldReturn_BadRequest_WhenFileIsNull()
    {
        // Arrange
        var captchaKey = Guid.NewGuid();
        _captchaCacheServiceMock
            .Setup(s => s.ValidateCaptchaAsync(captchaKey, "valid_captcha"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UploadFile(null, captchaKey, "valid_captcha");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid file", badRequestResult.Value);
    }

    [Fact]
    public async Task UploadFile_ShouldReturn_Ok_WhenFileIsUploadedSuccessfully()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write("Test file content");
        writer.Flush();
        stream.Position = 0;

        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.Length).Returns(stream.Length);
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

        var captchaKey = Guid.NewGuid();
        var fileUrl = "https://storage.example.com/test.jpg";

        _captchaCacheServiceMock
            .Setup(s => s.ValidateCaptchaAsync(captchaKey, "valid_captcha"))
            .ReturnsAsync(true);

        _fileStorageServiceMock
            .Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(fileUrl);

        _fileAttachmentServiceMock
            .Setup(s => s.AddFileAsync(It.IsAny<FileAttachment>()))
            .ReturnsAsync(new FileAttachment { Id = Guid.NewGuid(), CommentId = Guid.NewGuid(), Url = fileUrl, Type = FileType.Image});

        _commentRepositoryMock
            .Setup(s => s.UpdateHasAttachmentAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UploadFile(fileMock.Object, captchaKey, "valid_captcha");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(fileUrl, okResult.Value);
    }

    [Fact]
    public async Task UploadFile_ShouldReturn_BadRequest_WhenFileUploadFails()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var stream = new MemoryStream();
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.Length).Returns(stream.Length);
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

        var captchaKey = Guid.NewGuid();

        _captchaCacheServiceMock
            .Setup(s => s.ValidateCaptchaAsync(captchaKey, "valid_captcha"))
            .ReturnsAsync(true);

        _fileStorageServiceMock
            .Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync((string)null);

        // Act
        var result = await _controller.UploadFile(fileMock.Object, captchaKey, "valid_captcha");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Error uploading file", badRequestResult.Value);
    }
}
