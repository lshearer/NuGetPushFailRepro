using System.Collections.Generic;
using System.Threading.Tasks;

namespace NugetPushIssueRepro.StaticAssets
{
    internal interface IStaticAssetUploader
    {
        Task PublishAssets(IEnumerable<StaticAsset> assets);
    }
}