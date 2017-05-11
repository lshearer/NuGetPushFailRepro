using System.Collections.Generic;
using System.Linq;

namespace NugetPushIssueRepro.StaticAssets
{
    internal class StaticAssetManifest
    {
        public StaticAssetManifest(List<StaticAsset> assets)
        {
            Files = assets.ToDictionary(asset => asset.RelativePath);
        }

        public Dictionary<string, StaticAsset> Files { get; }
    }
}