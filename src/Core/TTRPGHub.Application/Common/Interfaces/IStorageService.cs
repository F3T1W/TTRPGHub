namespace TTRPGHub.Common.Interfaces;

public interface IStorageService
{
    Task<string> UploadAsync(string bucketName, string objectName, Stream data, string contentType,
        CancellationToken ct = default);
    Task EnsureBucketExistsAsync(string bucketName, CancellationToken ct = default);
}
