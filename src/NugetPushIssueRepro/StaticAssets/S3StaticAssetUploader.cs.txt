using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using NugetPushIssueRepro.Utility;

namespace NugetPushIssueRepro.StaticAssets
{
    internal class S3StaticAssetUploader : IStaticAssetUploader
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IAccessor<S3AssetHostConfiguration> _hostConfig;
        private readonly HttpClient _httpClient;
        private readonly IFileSystem _fileSystem;
        private readonly IConsole _console;

        public S3StaticAssetUploader(IAmazonS3 s3Client, IAccessor<S3AssetHostConfiguration> hostConfig, HttpClient httpClient, IFileSystem fileSystem, IConsole console)
        {
            _s3Client = s3Client.ThrowIfNull(nameof(s3Client));
            _hostConfig = hostConfig.ThrowIfNull(nameof(hostConfig));
            _httpClient = httpClient.ThrowIfNull(nameof(httpClient));
            _fileSystem = fileSystem.ThrowIfNull(nameof(fileSystem));
            _console = console.ThrowIfNull(nameof(console)); ;
        }

        public async Task PublishAssets(IEnumerable<StaticAsset> assets)
        {
            if (_hostConfig.Value.AwsRegion != _s3Client.Config.RegionEndpoint)
            {
                throw new InvalidOperationException("Mismatched region configurations.");
            }

            // Publish in parallel
            await Task.WhenAll(assets.Select(asset => PublishAsset(asset)));
        }

        private async Task PublishAsset(StaticAsset asset)
        {
            // Check for existing objects and pull metadata and tags
            (var metadata, var existingTags) = await GetExistingObjectMetadataAndTags(asset.Key);

            var tagsToSet = CreateTagSet(new Dictionary<string, string>
            {
                ["LastBuildTime"] = DateTime.UtcNow.ToString(),
            });

            var bucketName = _hostConfig.Value.BucketName;
            if (metadata == null)
            {
                // Upload missing objects with appropriate headers
                var request = new PutObjectRequest()
                {
                    BucketName = bucketName,
                    Key = asset.Key,
                    InputStream = _fileSystem.OpenRead(asset.FullPath),
                    TagSet = tagsToSet
                };

                request.Headers.CacheControl = "public, max-age=31536000, immutable";
                await _s3Client.PutObjectAsync(request);
                await _s3Client.MakeObjectPublicAsync(bucketName, asset.Key, true);
            }
            else
            {
                // Update existing tags
                tagsToSet.AddRange(existingTags.Tagging.Where(tag => tagsToSet.Any(newTag => newTag.Key == tag.Key)));
                await _s3Client.PutObjectTaggingAsync(new PutObjectTaggingRequest
                {
                    BucketName = bucketName,
                    Key = asset.Key,
                    Tagging = { TagSet = tagsToSet }
                });
            }

            // Make public HTTP requests for resources
            var response = await _httpClient.GetAsync(asset.Url);
            response.EnsureSuccessStatusCode();

            // Verify response headers
            // TODO

        }

        private List<Tag> CreateTagSet(Dictionary<string, string> tags)
        {
            return tags.Select(tag => new Tag
            {
                Key = tag.Key,
                Value = tag.Value,
            }).ToList();
        }

        private async Task<(GetObjectMetadataResponse, GetObjectTaggingResponse)> GetExistingObjectMetadataAndTags(string key)
        {
            string bucketName = _hostConfig.Value.BucketName;
            try
            {
                var metadata = await _s3Client.GetObjectMetadataAsync(bucketName, key);
                // Only requesting the tags if object exists
                var tags = await _s3Client.GetObjectTaggingAsync(new GetObjectTaggingRequest
                {
                    BucketName = bucketName,
                    Key = key,
                });
                return (metadata, tags);
            }
            catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                // Object doesn't exist
                return (null, null);
            }
            catch (AmazonS3Exception e)
            {
                throw new Exception($"Unexpected exception encountered while retrieving S3 object. Bucket={bucketName} Key={key}", e);
            }
        }
    }
}