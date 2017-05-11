using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NugetPushIssueRepro.Utility;

namespace NugetPushIssueRepro.StaticAssets
{
    internal class S3StaticAssetHost : IStaticAssetHost
    {
        private readonly IAccessor<S3AssetHostConfiguration> _configAccessor;

        public S3StaticAssetHost(IAccessor<S3AssetHostConfiguration> assetHostConfigurationAccessor)
        {
            _configAccessor = assetHostConfigurationAccessor.ThrowIfNull(nameof(assetHostConfigurationAccessor));
        }

        public string GetUrlForAssetKey(string key)
        {
            return $"https://{_configAccessor.Value.PublicHostName}/{key}";
        }
    }
}