using Google.Cloud.Storage.V1;
using GoogleObject = Google.Apis.Storage.v1.Data.Object;

namespace BuildingBlocks.GoogleStorage;

internal sealed class GoogleCloudFileStorage(StorageClient client, UrlSigner urlSigner, string bucketName) : IFileStorage
{
    public async Task<StoredFile> UploadAsync(
        Guid tenantId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead) throw new ArgumentException("The file stream must be readable.", nameof(content));

        var safeFileName = Path.GetFileName(fileName);
        var extension = Path.GetExtension(safeFileName);
        var objectName = $"tenants/{tenantId:D}/{Guid.NewGuid():N}{extension}";
        var stored = await client.UploadObjectAsync(
            new GoogleObject { Bucket = bucketName, Name = objectName, ContentType = contentType, ContentDisposition = "attachment" },
            content,
            cancellationToken: cancellationToken);

        return new StoredFile(objectName, safeFileName, contentType, stored.Size ?? 0);
    }

    public async Task<ResumableUploadSession> StartResumableUploadAsync(
        Guid tenantId,
        string fileName,
        string contentType,
        long? contentLength = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        if (contentLength is < 0) throw new ArgumentOutOfRangeException(nameof(contentLength));

        var extension = Path.GetExtension(Path.GetFileName(fileName));
        var objectName = $"tenants/{tenantId:D}/{Guid.NewGuid():N}{extension}";
        var uploadUri = await client.InitiateUploadSessionAsync(
            bucketName,
            objectName,
            contentType,
            contentLength,
            cancellationToken: cancellationToken);

        return new ResumableUploadSession(objectName, uploadUri);
    }

    public Task CopyToAsync(Guid tenantId, string objectName, Stream destination, CancellationToken cancellationToken = default)
    {
        ValidateTenantObject(tenantId, objectName);
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite) throw new ArgumentException("The destination stream must be writable.", nameof(destination));
        return client.DownloadObjectAsync(bucketName, objectName, destination, cancellationToken: cancellationToken);
    }

    public async Task<Uri> CreateDownloadUrlAsync(Guid tenantId, string objectName, TimeSpan expiresIn, CancellationToken cancellationToken = default)
    {
        ValidateTenantObject(tenantId, objectName);
        if (expiresIn <= TimeSpan.Zero || expiresIn > TimeSpan.FromMinutes(15))
            throw new ArgumentOutOfRangeException(nameof(expiresIn), "Download URLs must expire within 15 minutes.");

        var signedUrl = await urlSigner.SignAsync(
            bucketName,
            objectName,
            expiresIn,
            HttpMethod.Get,
            SigningVersion.V4,
            cancellationToken);
        return new Uri(signedUrl, UriKind.Absolute);
    }

    public Task DeleteAsync(Guid tenantId, string objectName, CancellationToken cancellationToken = default)
    {
        ValidateTenantObject(tenantId, objectName);
        return client.DeleteObjectAsync(bucketName, objectName, cancellationToken: cancellationToken);
    }

    private static void ValidateTenantObject(Guid tenantId, string objectName)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectName);
        var expectedPrefix = $"tenants/{tenantId:D}/";
        if (!objectName.StartsWith(expectedPrefix, StringComparison.Ordinal))
            throw new ArgumentException("The object does not belong to the specified tenant.", nameof(objectName));
    }
}
