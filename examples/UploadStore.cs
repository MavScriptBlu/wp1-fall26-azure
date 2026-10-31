// Add current stable Azure.Identity and Azure.Storage.Blobs NuGet packages.
// Register UploadStore as a service; invoke only after your app authenticates and authorizes the caller.
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

/// <summary>
/// Provides secure, private file storage in Azure Blob Storage.
/// Authenticates via Azure Managed Identity so no storage keys or connection strings are needed.
/// </summary>
public sealed class UploadStore
{
    private readonly BlobContainerClient container;

    // Initialize Blob client using passwordless DefaultAzureCredential.
    // In Azure Container Apps: Uses the assigned runtime Managed Identity (runtimeClientId).
    // In local development: Falls back to your Azure CLI or Visual Studio credentials.
    public UploadStore(string blobServiceUri, string containerName, string? runtimeClientId)
    {
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = runtimeClientId
        });

        container = new BlobServiceClient(new Uri(blobServiceUri), credential)
            .GetBlobContainerClient(containerName);
    }

    // Upload a stream to private Blob Storage and return a unique storage key
    public async Task<string> SaveAsync(Stream content, long length, CancellationToken ct = default)
    {
        // Enforce 5 MiB maximum upload size limit to protect memory and container quotas
        if (length < 1 || length > 5 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(length), "Upload limit is 5 MiB.");

        // Generate an unguessable random GUID key so user filenames cannot overwrite blobs or leak paths
        string key = Guid.NewGuid().ToString("N");

        // Upload stream content directly to Azure Blob Storage with generic binary content type
        await container.GetBlobClient(key).UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "application/octet-stream" }
        }, ct);

        // Return key for saving in the database alongside the authenticated user's ID.
        // Blobs remain private and are never exposed via direct public URLs.
        return key;
    }

    // Download a stored blob by its key and stream it to the destination output
    public async Task DownloadAsync(string key, Stream destination, CancellationToken ct = default)
    {
        // Calling endpoint must verify caller authorization before streaming the blob
        await container.GetBlobClient(key).DownloadToAsync(destination, ct);
    }
}
