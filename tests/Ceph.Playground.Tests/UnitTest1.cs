using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ceph.Playground.Tests;

public class CephFixture : IAsyncLifetime
{
    public string BucketName { get; } = $"test-{Guid.NewGuid():N}"[..20];
    public HttpClient Client { get; private set; } = null!;
    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>();
        Client = _factory.CreateClient();

        var response = await Client.PostAsync($"/api/buckets/{BucketName}", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }
}

public class FileManagementIntegrationTests : IClassFixture<CephFixture>
{
    private readonly CephFixture _fixture;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public FileManagementIntegrationTests(CephFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateBucketWithVersioning_Succeeds()
    {
        var bucket = $"vtest-{Guid.NewGuid():N}"[..20];
        var response = await _fixture.Client.PostAsync($"/api/buckets/{bucket}", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(bucket, body.GetProperty("bucket").GetString());
        Assert.Equal("Enabled", body.GetProperty("versioning").GetString());
    }

    [Fact]
    public async Task UploadFile_ReturnsCreatedWithVersionId()
    {
        var key = $"upload-{Guid.NewGuid():N}.txt";
        var content = new StringContent("hello world v1", Encoding.UTF8, "text/plain");

        var response = await _fixture.Client.PostAsync(
            $"/api/files/{_fixture.BucketName}/{key}", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(_fixture.BucketName, body.GetProperty("bucket").GetString());
        Assert.Equal(key, body.GetProperty("key").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("versionId").GetString()));
    }

    [Fact]
    public async Task UpdateFile_CreatesNewVersion()
    {
        var key = $"versioned-{Guid.NewGuid():N}.txt";

        // Upload v1
        var content1 = new StringContent("version 1 content", Encoding.UTF8, "text/plain");
        var response1 = await _fixture.Client.PostAsync(
            $"/api/files/{_fixture.BucketName}/{key}", content1);
        var body1 = await response1.Content.ReadFromJsonAsync<JsonElement>();
        var versionId1 = body1.GetProperty("versionId").GetString();

        // Update to v2
        var content2 = new StringContent("version 2 content", Encoding.UTF8, "text/plain");
        var response2 = await _fixture.Client.PutAsync(
            $"/api/files/{_fixture.BucketName}/{key}", content2);
        var body2 = await response2.Content.ReadFromJsonAsync<JsonElement>();
        var versionId2 = body2.GetProperty("versionId").GetString();

        Assert.NotEqual(versionId1, versionId2);
    }

    [Fact]
    public async Task ListVersions_ReturnsAllVersions()
    {
        var key = $"multi-{Guid.NewGuid():N}.txt";

        // Create 3 versions
        for (int i = 1; i <= 3; i++)
        {
            var content = new StringContent($"version {i}", Encoding.UTF8, "text/plain");
            await _fixture.Client.PostAsync(
                $"/api/files/{_fixture.BucketName}/{key}", content);
        }

        var response = await _fixture.Client.GetAsync(
            $"/api/files/{_fixture.BucketName}/{key}/versions");
        response.EnsureSuccessStatusCode();

        var versions = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(versions);
        Assert.Equal(3, versions.Length);

        // Exactly one should be marked as latest
        Assert.Single(versions, v => v.GetProperty("isLatest").GetBoolean());
    }

    [Fact]
    public async Task GetPresignedUrl_ReturnsValidUrl()
    {
        var key = $"presigned-{Guid.NewGuid():N}.txt";
        var content = new StringContent("presigned test content", Encoding.UTF8, "text/plain");
        await _fixture.Client.PostAsync(
            $"/api/files/{_fixture.BucketName}/{key}", content);

        var response = await _fixture.Client.GetAsync(
            $"/api/files/{_fixture.BucketName}/{key}/download-url");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var url = body.GetProperty("url").GetString();

        Assert.NotNull(url);
        Assert.Contains(_fixture.BucketName, url);
        Assert.Contains(key, url);
        Assert.Contains("Signature=", url);
    }

    [Fact]
    public async Task PresignedUrl_CanDownloadFileContent()
    {
        var key = $"download-{Guid.NewGuid():N}.txt";
        var expectedContent = "download me via presigned url";
        var content = new StringContent(expectedContent, Encoding.UTF8, "text/plain");
        await _fixture.Client.PostAsync(
            $"/api/files/{_fixture.BucketName}/{key}", content);

        // Get presigned URL
        var urlResponse = await _fixture.Client.GetAsync(
            $"/api/files/{_fixture.BucketName}/{key}/download-url");
        var urlBody = await urlResponse.Content.ReadFromJsonAsync<JsonElement>();
        var presignedUrl = urlBody.GetProperty("url").GetString()!;

        // Download directly from Ceph RGW via presigned URL
        using var directClient = new HttpClient();
        var downloadResponse = await directClient.GetAsync(presignedUrl);
        downloadResponse.EnsureSuccessStatusCode();
        var downloadedContent = await downloadResponse.Content.ReadAsStringAsync();

        Assert.Equal(expectedContent, downloadedContent);
    }

    [Fact]
    public async Task PresignedUrlForSpecificVersion_DownloadsCorrectContent()
    {
        var key = $"ver-dl-{Guid.NewGuid():N}.txt";

        // Upload v1
        var content1 = new StringContent("first version", Encoding.UTF8, "text/plain");
        var response1 = await _fixture.Client.PostAsync(
            $"/api/files/{_fixture.BucketName}/{key}", content1);
        var body1 = await response1.Content.ReadFromJsonAsync<JsonElement>();
        var versionId1 = body1.GetProperty("versionId").GetString()!;

        // Upload v2
        var content2 = new StringContent("second version", Encoding.UTF8, "text/plain");
        await _fixture.Client.PutAsync(
            $"/api/files/{_fixture.BucketName}/{key}", content2);

        // Get presigned URL for v1 specifically
        var urlResponse = await _fixture.Client.GetAsync(
            $"/api/files/{_fixture.BucketName}/{key}/download-url?versionId={versionId1}");
        var urlBody = await urlResponse.Content.ReadFromJsonAsync<JsonElement>();
        var presignedUrl = urlBody.GetProperty("url").GetString()!;

        // Download v1 via presigned URL
        using var directClient = new HttpClient();
        var downloadResponse = await directClient.GetAsync(presignedUrl);
        downloadResponse.EnsureSuccessStatusCode();
        var downloadedContent = await downloadResponse.Content.ReadAsStringAsync();

        Assert.Equal("first version", downloadedContent);
    }
}
