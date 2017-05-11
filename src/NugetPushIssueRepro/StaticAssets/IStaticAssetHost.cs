using System.Collections.Generic;
using System.Threading.Tasks;

namespace NugetPushIssueRepro.StaticAssets
{
    internal interface IStaticAssetHost
    {
        string GetUrlForAssetKey(string key);
    }
}