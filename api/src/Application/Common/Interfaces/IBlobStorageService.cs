namespace api.Application.Common.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    Uri GenerateSasUri(
        string containerName,
        string blobName,
        TimeSpan validity);

    /// <summary>
    /// Verifies that a blob URL belongs to the specified recruitment by checking
    /// the normalized path starts with "{recruitmentId}/". Prevents path traversal attacks.
    /// </summary>
    bool VerifyBlobOwnership(string containerName, string blobUrl, Guid recruitmentId);
}
