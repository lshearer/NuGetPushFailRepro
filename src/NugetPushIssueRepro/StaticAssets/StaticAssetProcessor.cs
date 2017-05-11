using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NugetPushIssueRepro.Utility;
using Microsoft.Extensions.FileProviders;

namespace NugetPushIssueRepro.StaticAssets
{
    internal class StaticAssetProcessor : IStaticAssetProcessor
    {
        /// <summary>
        /// The length used to trim the contents hash for a static asset, to shorten the excessively long hashes. This can be lengthened if we
        /// see any builds failing from collisions, although this isn't likely.
        /// </summary>
        private const int HashTrimLength = 7;
        private readonly IFileProvider _fileProvider;
        private readonly IHashUtility _hashUtility;
        private readonly IStaticAssetHost _staticAssetHost;
        private readonly IStaticAssetUploader _staticAssetUploader;
        private readonly IFileSystem _fileSystem;
        private readonly IStaticAssetManifestSerializer _staticAssetManifestSerializer;
        private readonly IConsole _console;

        public StaticAssetProcessor(
            IFileProvider fileProvider,
            IHashUtility hashUtility,
            IStaticAssetHost staticAssetHost,
            IStaticAssetUploader staticAssetUploader,
            IFileSystem fileSystem,
            IStaticAssetManifestSerializer staticAssetManifestSerializer,
            IConsole console)
        {
            _fileProvider = fileProvider.ThrowIfNull(nameof(fileProvider));
            _hashUtility = hashUtility.ThrowIfNull(nameof(hashUtility));
            _staticAssetHost = staticAssetHost.ThrowIfNull(nameof(staticAssetHost));
            _staticAssetUploader = staticAssetUploader.ThrowIfNull(nameof(staticAssetUploader));
            _fileSystem = fileSystem.ThrowIfNull(nameof(fileSystem));
            _staticAssetManifestSerializer = staticAssetManifestSerializer.ThrowIfNull(nameof(staticAssetManifestSerializer));
            _console = console.ThrowIfNull(nameof(console));
        }

        public async Task ProcessStaticAssets(string publicWebRootDirectory)
        {
            _console.Info("Processing static assets...");

            var assetPaths = GetStaticAssetPaths(_fileProvider, publicWebRootDirectory);

            var assets = assetPaths.Select(asset =>
            {
                var hash = _hashUtility.HashData(File.ReadAllText(asset.PhysicalPath));
                var relativePath = GetRelativePath(publicWebRootDirectory, asset.PhysicalPath);
                var objectKey = CreateAssetKey(relativePath, hash);
                var cdnUrl = _staticAssetHost.GetUrlForAssetKey(objectKey);
                return new StaticAsset
                {
                    FullPath = asset.PhysicalPath,
                    Hash = hash,
                    RelativePath = relativePath,
                    Url = cdnUrl,
                    Key = objectKey,
                };
            }).ToList();

            _console.Info($"{assets.Count} static assets found.");


            // Publish to S3
            _console.Info("Publishing assets to S3...");
            await _staticAssetUploader.PublishAssets(assets);

            _console.Info("Deleting original assets from payload...");
            // Remove assets to trim waste and ensure we are always loading them from the CDN.
            foreach (var asset in assets)
            {
                _fileSystem.Delete(asset.FullPath);
            }

            // Write manifest file
            var serializedManifest = _staticAssetManifestSerializer.SerializeManifest(new StaticAssetManifest(assets));

            string manifestFilePath = Path.Combine(publicWebRootDirectory, "static-assets.json");
            _console.Info($"Writing manifest file to  {manifestFilePath}...");

            _fileSystem.CreateDirectory(publicWebRootDirectory);
            _fileSystem.WriteAllText(manifestFilePath, serializedManifest);
        }


        internal static string GetRelativePath(string basePath, string filePath)
        {
            // Normalize absolute paths
            filePath = Path.GetFullPath(filePath);
            basePath = Path.GetFullPath(basePath);

            if (!filePath.StartsWith(basePath))
            {
                throw new InvalidOperationException("Cannot get relative path. Base path must contain file path.")
                    .WithData("FilePath", filePath)
                    .WithData("BasePath", basePath);
            }

            return NormalizeSlashes(filePath.Substring(basePath.Length)).TrimStart('/');
        }

        internal static string NormalizeSlashes(string path)
        {
            return path.Replace('\\', '/');
        }

        internal static string CreateAssetKey(string relativePath, string hash)
        {
            var fileName = Path.GetFileName(relativePath);
            // http://docs.aws.amazon.com/AmazonS3/latest/dev/UsingMetadata.html#object-key-guidelines-safe-characters
            fileName = Regex.Replace(fileName, "[^0-9a-zA-Z!_.*'()-]", "_");
            return $"{hash.Substring(0, HashTrimLength)}/{fileName}";
        }

        internal static List<IFileInfo> GetStaticAssetPaths(IFileProvider fileProvider, string publicWebRootDirectory)
        {
            // Load files
            var files = fileProvider.GetFilesRecursive(publicWebRootDirectory);

            return files.ToList();
        }
    }
}