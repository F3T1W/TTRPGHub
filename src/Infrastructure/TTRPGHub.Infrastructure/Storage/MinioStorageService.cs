using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using TTRPGHub.Common.Interfaces;

namespace TTRPGHub.Storage;

internal sealed class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minio;
    private readonly string _publicEndpoint;

    public MinioStorageService(IConfiguration configuration)
    {
        var endpoint  = configuration["Storage:Endpoint"]  ?? "localhost:9000";
        var accessKey = configuration["Storage:AccessKey"] ?? "taverna";
        var secretKey = configuration["Storage:SecretKey"] ?? "taverna_dev_pass";
        _publicEndpoint = configuration["Storage:PublicEndpoint"] ?? $"http://{endpoint}";

        _minio = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .Build();
    }

    public async Task EnsureBucketExistsAsync(string bucketName, CancellationToken ct = default)
    {
        var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName), ct);
        if (!exists)
        {
            await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName), ct);
            await SetBucketPublicReadAsync(bucketName, ct);
        }
    }

    public async Task<string> UploadAsync(
        string bucketName, string objectName,
        Stream data, string contentType,
        CancellationToken ct = default)
    {
        var args = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(data)
            .WithObjectSize(data.Length)
            .WithContentType(contentType);

        await _minio.PutObjectAsync(args, ct);
        return $"{_publicEndpoint}/{bucketName}/{objectName}";
    }

    private async Task SetBucketPublicReadAsync(string bucketName, CancellationToken ct)
    {
        var policy = $$"""
            {
              "Version": "2012-10-17",
              "Statement": [{
                "Effect": "Allow",
                "Principal": "*",
                "Action": ["s3:GetObject"],
                "Resource": ["arn:aws:s3:::{{bucketName}}/*"]
              }]
            }
            """;

        await _minio.SetPolicyAsync(
            new SetPolicyArgs().WithBucket(bucketName).WithPolicy(policy), ct);
    }
}
