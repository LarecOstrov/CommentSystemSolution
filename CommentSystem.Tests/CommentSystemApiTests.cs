using System.Net;
using System.Net.Http.Json;
using Xunit;
using CommentSystem;
using Microsoft.AspNetCore.Mvc.Testing;
using Common.Models.Inputs;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.TestHost;

public class CommentSystemApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CommentSystemApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostComment_ShouldReturn_Ok()
    {
        // Arrange: create a new GraphQL request
        var requestData = new
        {
            query = @"mutation ($input: AddCommentInput!) {
                        addComment(input: $input) {
                            id
                            text
                            user {
                                userName
                                email
                            }
                        }
                    }",
            variables = new
            {
                input = new
                {
                    UserName = "Test User",
                    Email = "example@example.com",
                    HomePage = "http://example.com",
                    Text = "Test Comment",
                    CaptchaKey = Guid.NewGuid().ToString(),
                    Captcha = "Test Captcha",
                    HasAttachment = false
                }
            }
        };

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/graphql", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("addComment", responseContent);
        Assert.Contains("Test Comment", responseContent);
    }
}
