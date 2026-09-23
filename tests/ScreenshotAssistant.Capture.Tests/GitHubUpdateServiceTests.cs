using System.Net;
using System.Text;
using ScreenshotAssistant.Capture;

namespace ScreenshotAssistant.Capture.Tests;

public sealed class GitHubUpdateServiceTests
{
    [Fact]
    public async Task CheckAsync_ReturnsAvailable_WhenReleaseIsNewer()
    {
        using HttpClient client = CreateClient(
            HttpStatusCode.OK,
            """{"tag_name":"v0.2.0","html_url":"https://github.com/mercyjason/SnapAnnotate/releases/tag/v0.2.0"}""");
        GitHubUpdateService service = new(client, "mercyjason", "SnapAnnotate", new Version(0, 1, 0));

        UpdateCheckResult result = await service.CheckAsync(TestContext.Current.CancellationToken);

        Assert.Equal(UpdateCheckStatus.UpdateAvailable, result.Status);
        Assert.Equal(new Version(0, 2, 0), result.LatestVersion);
        Assert.NotNull(result.ReleasePage);
    }

    [Fact]
    public async Task CheckAsync_ReturnsCurrent_WhenVersionsMatch()
    {
        using HttpClient client = CreateClient(
            HttpStatusCode.OK,
            """{"tag_name":"0.1.0","html_url":"https://github.com/mercyjason/SnapAnnotate/releases/tag/v0.1.0"}""");
        GitHubUpdateService service = new(client, "mercyjason", "SnapAnnotate", new Version(0, 1, 0));

        UpdateCheckResult result = await service.CheckAsync(TestContext.Current.CancellationToken);

        Assert.Equal(UpdateCheckStatus.Current, result.Status);
    }

    [Fact]
    public async Task CheckAsync_ReturnsUnavailable_WhenNoReleaseExists()
    {
        using HttpClient client = CreateClient(HttpStatusCode.NotFound, "{}");
        GitHubUpdateService service = new(client, "mercyjason", "SnapAnnotate", new Version(0, 1, 0));

        UpdateCheckResult result = await service.CheckAsync(TestContext.Current.CancellationToken);

        Assert.Equal(UpdateCheckStatus.Unavailable, result.Status);
        Assert.Contains("尚未发布", result.Message);
    }

    [Theory]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("1.2.3-beta.1", "1.2.3")]
    public void TryParseVersion_AcceptsCommonGitHubTags(string tag, string expected)
    {
        bool parsed = GitHubUpdateService.TryParseVersion(tag, out Version? version);

        Assert.True(parsed);
        Assert.Equal(Version.Parse(expected), version);
    }

    private static HttpClient CreateClient(HttpStatusCode statusCode, string json) =>
        new(new StubHandler(statusCode, json));

    private sealed class StubHandler(HttpStatusCode statusCode, string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Assert.Equal("SnapAnnotate-UpdateChecker/1.0", request.Headers.UserAgent.ToString());
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
