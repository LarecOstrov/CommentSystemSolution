using Xunit;
using Moq;
using CaptchaServiceAPI.Services.Interfaces;
using CaptchaServiceAPI.Controllers;
using CaptchaServiceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Common.Services.Interfaces;

namespace CaptchaServiceAPI.Tests;
public class CaptchaServiceApiTests
{
    private readonly Mock<ICaptchaService> _captchaServiceMock;
    private readonly Mock<ICaptchaCacheService> _captchaCacheServiceMock;
    private readonly CaptchaController _controller;

    public CaptchaServiceApiTests()
    {
        _captchaServiceMock = new Mock<ICaptchaService>();
        _captchaCacheServiceMock = new Mock<ICaptchaCacheService>();
        _controller = new CaptchaController(_captchaServiceMock.Object, _captchaCacheServiceMock.Object);
    }

    [Fact]
    public async Task GenerateCaptcha_ShouldReturn_CaptchaImageAndKey()
    {
        // Arrange
        var captchaImage = new byte[] { 1, 2, 3 };
        var captchaKey = Guid.NewGuid();
        _captchaServiceMock
            .Setup(service => service.GenerateCaptchaAsync())
            .ReturnsAsync((captchaImage, captchaKey));

        // Act
        var result = await _controller.GenerateCaptcha();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseData = okResult.Value as dynamic;
        Assert.NotNull(responseData);
        Assert.NotNull(responseData?.image);
        Assert.Equal(captchaKey, responseData?.captchaKey);
    }

    [Fact]
    public async Task ValidateCaptcha_ValidInput_ShouldReturn_True()
    {
        // Arrange
        var request = new CaptchaValidationRequest
        {
            CaptchaKey = Guid.NewGuid(),
            UserInput = "12345"
        };
        _captchaCacheServiceMock
            .Setup(service => service.ValidateCaptchaAsync(request.CaptchaKey, request.UserInput))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ValidateCaptcha(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseData = okResult.Value as dynamic;
        Assert.True(responseData?.isValid);
    }

    [Fact]
    public async Task ValidateCaptcha_InvalidInput_ShouldReturn_False()
    {
        // Arrange
        var request = new CaptchaValidationRequest
        {
            CaptchaKey = Guid.NewGuid(),
            UserInput = "wrong"
        };
        _captchaCacheServiceMock
            .Setup(service => service.ValidateCaptchaAsync(request.CaptchaKey, request.UserInput))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ValidateCaptcha(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var responseData = badRequestResult.Value as dynamic;
        Assert.False(responseData?.isValid);
    }
}
