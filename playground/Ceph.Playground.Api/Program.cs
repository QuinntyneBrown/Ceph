using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

var builder = WebApplication.CreateBuilder(args);

var cephSection = builder.Configuration.GetSection("Ceph");
builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var s3Config = new AmazonS3Config
    {
        ServiceURL = cephSection["ServiceUrl"] ?? "http://localhost:7480",
        ForcePathStyle = true,
        UseHttp = true
    };
    var credentials = new BasicAWSCredentials(
        cephSection["AccessKey"] ?? "demo-access-key",
        cephSection["SecretKey"] ?? "demo-secret-key");
    return new AmazonS3Client(credentials, s3Config);
});

var app = builder.Build();

// --- Bucket endpoints ---

app.MapPost("/api/buckets/{bucket}", async (string bucket, IAmazonS3 s3) =>
{
    await s3.PutBucketAsync(bucket);
    await s3.PutBucketVersioningAsync(new PutBucketVersioningRequest
    {
        BucketName = bucket,
        VersioningConfig = new S3BucketVersioningConfig { Status = VersionStatus.Enabled }
    });
    return Results.Created($"/api/buckets/{bucket}", new { Bucket = bucket, Versioning = "Enabled" });
});

app.MapGet("/api/buckets", async (IAmazonS3 s3) =>
{
    var response = await s3.ListBucketsAsync();
    return Results.Ok(response.Buckets.Select(b => new { b.BucketName, b.CreationDate }));
});

// --- File endpoints ---

app.MapPost("/api/files/{bucket}/{key}", async (string bucket, string key, HttpRequest request, IAmazonS3 s3) =>
{
    using var ms = new MemoryStream();
    await request.Body.CopyToAsync(ms);
    ms.Position = 0;

    var putRequest = new PutObjectRequest
    {
        BucketName = bucket,
        Key = key,
        InputStream = ms,
        ContentType = request.ContentType ?? "application/octet-stream",
        UseChunkEncoding = false
    };
    var response = await s3.PutObjectAsync(putRequest);

    return Results.Created($"/api/files/{bucket}/{key}", new { Bucket = bucket, Key = key, VersionId = response.VersionId });
});

app.MapPut("/api/files/{bucket}/{key}", async (string bucket, string key, HttpRequest request, IAmazonS3 s3) =>
{
    using var ms = new MemoryStream();
    await request.Body.CopyToAsync(ms);
    ms.Position = 0;

    var putRequest = new PutObjectRequest
    {
        BucketName = bucket,
        Key = key,
        InputStream = ms,
        ContentType = request.ContentType ?? "application/octet-stream",
        UseChunkEncoding = false
    };
    var response = await s3.PutObjectAsync(putRequest);

    return Results.Ok(new { Bucket = bucket, Key = key, VersionId = response.VersionId });
});

app.MapGet("/api/files/{bucket}/{key}/versions", async (string bucket, string key, IAmazonS3 s3) =>
{
    var response = await s3.ListVersionsAsync(new ListVersionsRequest
    {
        BucketName = bucket,
        Prefix = key
    });

    var versions = response.Versions
        .Where(v => v.Key == key)
        .Select(v => new
        {
            v.VersionId,
            v.LastModified,
            v.Size,
            v.IsLatest,
            v.ETag
        });

    return Results.Ok(versions);
});

app.MapGet("/api/files/{bucket}/{key}/download-url", (string bucket, string key, string? versionId, IAmazonS3 s3) =>
{
    var request = new GetPreSignedUrlRequest
    {
        BucketName = bucket,
        Key = key,
        Expires = DateTime.UtcNow.AddHours(1),
        Verb = HttpVerb.GET,
        Protocol = Protocol.HTTP
    };

    if (!string.IsNullOrEmpty(versionId))
        request.VersionId = versionId;

    var url = s3.GetPreSignedURL(request);
    return Results.Ok(new { Url = url, ExpiresInHours = 1 });
});

app.MapDelete("/api/files/{bucket}/{key}", async (string bucket, string key, string? versionId, IAmazonS3 s3) =>
{
    var deleteRequest = new DeleteObjectRequest
    {
        BucketName = bucket,
        Key = key
    };

    if (!string.IsNullOrEmpty(versionId))
        deleteRequest.VersionId = versionId;

    await s3.DeleteObjectAsync(deleteRequest);
    return Results.NoContent();
});

app.Run();

public partial class Program { }
