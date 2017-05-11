using System.Threading.Tasks;

namespace NugetPushIssueRepro.StaticAssets
{
    internal interface IStaticAssetProcessor
    {
        Task ProcessStaticAssets(string publicWebRootDirectory);
    }
}