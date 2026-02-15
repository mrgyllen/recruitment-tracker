using api.Application.Common.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace api.Infrastructure.Services;

public class BlobStorageService(BlobServiceClient blobServiceClient) : IBlobStorageService
{
    public async Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken: cancellationToken);
        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<Stream> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    private static readonly TimeSpan MaxSasValidity = TimeSpan.FromMinutes(15);

    public bool VerifyBlobOwnership(string containerName, string blobUrl, Guid recruitmentId)
    {
        if (string.IsNullOrEmpty(blobUrl))
            return false;

        // Normalize the path: resolve any ../ segments
        var normalized = Path.GetFullPath(blobUrl).Replace('\\', '/');

        // Also normalize without filesystem context — strip ../ segments directly
        var segments = blobUrl.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var stack = new Stack<string>();
        foreach (var segment in segments)
        {
            if (segment == "..")
            {
                if (stack.Count > 0) stack.Pop();
            }
            else if (segment != ".")
            {
                stack.Push(segment);
            }
        }
        var cleanPath = string.Join("/", stack.Reverse());

        var expectedPrefix = recruitmentId.ToString() + "/";
        return cleanPath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase);
    }

    public Uri GenerateSasUri(
        string containerName,
        string blobName,
        TimeSpan validity)
    {
        if (validity > MaxSasValidity)
        {
            validity = MaxSasValidity;
        }

        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        return blobClient.GenerateSasUri(
            BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.Add(validity));
    }
}
