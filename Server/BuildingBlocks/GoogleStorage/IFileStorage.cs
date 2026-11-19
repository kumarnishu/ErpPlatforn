namespace BuildingBlocks.GoogleStorage;

/// <summary>Details about a stored file.</summary>
public sealed record StoredFile(string ObjectName, string FileName, string ContentType, long SizeBytes);

/// <summary>URL for a resumable upload session.</summary>
public sealed record ResumableUploadSession(string ObjectName, Uri UploadUri);

/// <summary>Stores tenant-owned files in a private object store.</summary>
public interface IFileStorage
{
    /// <summary>Uploads a file for a tenant.</summary>
    Task<StoredFile> UploadAsync(Guid tenantId, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Starts a resumable upload for a large file.</summary>
    Task<ResumableUploadSession> StartResumableUploadAsync(Guid tenantId, string fileName, string contentType, long? contentLength = null, CancellationToken cancellationToken = default);

    /// <summary>Downloads a tenant's file into a stream.</summary>
    Task CopyToAsync(Guid tenantId, string objectName, Stream destination, CancellationToken cancellationToken = default);

    /// <summary>Creates a short-lived URL to download a tenant's file.</summary>
    Task<Uri> CreateDownloadUrlAsync(Guid tenantId, string objectName, TimeSpan expiresIn, CancellationToken cancellationToken = default);

    /// <summary>Deletes a tenant's file.</summary>
    Task DeleteAsync(Guid tenantId, string objectName, CancellationToken cancellationToken = default);
}
