using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ScreenshotAssistant.Capture;

public sealed class GitHubUpdateService
{
    private readonly HttpClient _httpClient;
    private readonly string _latestReleaseEndpoint;
    private readonly Version _currentVersion;

    public GitHubUpdateService(
        HttpClient httpClient,
        string repositoryOwner,
        string repositoryName,
        Version currentVersion)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryOwner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryName);
        ArgumentNullException.ThrowIfNull(currentVersion);

        _httpClient = httpClient;
        _latestReleaseEndpoint =
            $"https://api.github.com/repos/{repositoryOwner}/{repositoryName}/releases/latest";
        _currentVersion = currentVersion;
    }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, _latestReleaseEndpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.ParseAdd("SnapAnnotate-UpdateChecker/1.0");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

        try
        {
            using HttpResponseMessage response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return UpdateCheckResult.Unavailable("尚未发布 GitHub Release。");
            }

            response.EnsureSuccessStatusCode();
            await using Stream content = await response.Content.ReadAsStreamAsync(cancellationToken);
            GitHubRelease? release = await JsonSerializer.DeserializeAsync<GitHubRelease>(
                content,
                cancellationToken: cancellationToken);
            if (release is null ||
                !TryParseVersion(release.TagName, out Version? latestVersion) ||
                latestVersion is null ||
                !Uri.TryCreate(release.HtmlUrl, UriKind.Absolute, out Uri? releasePage) ||
                releasePage.Scheme != Uri.UriSchemeHttps)
            {
                return UpdateCheckResult.Unavailable("GitHub Release 的版本信息无效。");
            }

            return latestVersion > _currentVersion
                ? UpdateCheckResult.Available(latestVersion, releasePage)
                : UpdateCheckResult.Current(latestVersion);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return UpdateCheckResult.Unavailable("检查更新超时，请稍后重试。");
        }
        catch (HttpRequestException exception)
        {
            return UpdateCheckResult.Unavailable($"无法连接 GitHub：{exception.Message}");
        }
        catch (JsonException)
        {
            return UpdateCheckResult.Unavailable("GitHub 返回了无法识别的版本信息。");
        }
    }

    internal static bool TryParseVersion(string? tagName, out Version? version)
    {
        version = null;
        string value = tagName?.Trim() ?? "";
        if (value.StartsWith('v') || value.StartsWith('V'))
        {
            value = value[1..];
        }

        int metadataStart = value.IndexOfAny(['-', '+']);
        if (metadataStart >= 0)
        {
            value = value[..metadataStart];
        }

        return Version.TryParse(value, out version);
    }

    private sealed record GitHubRelease(
        [property: System.Text.Json.Serialization.JsonPropertyName("tag_name")] string? TagName,
        [property: System.Text.Json.Serialization.JsonPropertyName("html_url")] string? HtmlUrl);
}

public enum UpdateCheckStatus
{
    Current,
    UpdateAvailable,
    Unavailable
}

public sealed record UpdateCheckResult(
    UpdateCheckStatus Status,
    Version? LatestVersion,
    Uri? ReleasePage,
    string Message)
{
    public static UpdateCheckResult Current(Version version) =>
        new(UpdateCheckStatus.Current, version, null, $"当前已是最新版本（{version}）。");

    public static UpdateCheckResult Available(Version version, Uri releasePage) =>
        new(UpdateCheckStatus.UpdateAvailable, version, releasePage, $"发现新版本 {version}。");

    public static UpdateCheckResult Unavailable(string message) =>
        new(UpdateCheckStatus.Unavailable, null, null, message);
}
