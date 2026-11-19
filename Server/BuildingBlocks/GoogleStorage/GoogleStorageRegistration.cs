using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.GoogleStorage;

public static class GoogleStorageRegistration
{
    /// <summary>Adds Google Cloud Storage file storage.</summary>
    public static IServiceCollection AddGoogleCloudFileStorage(this IServiceCollection services, string bucketName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucketName);
        services.AddSingleton(_ => StorageClient.Create());
        services.AddSingleton(_ => UrlSigner.FromCredential(GoogleCredential.GetApplicationDefault()));
        services.AddSingleton<IFileStorage>(provider =>
            new GoogleCloudFileStorage(
                provider.GetRequiredService<StorageClient>(),
                provider.GetRequiredService<UrlSigner>(),
                bucketName));
        return services;
    }
}
