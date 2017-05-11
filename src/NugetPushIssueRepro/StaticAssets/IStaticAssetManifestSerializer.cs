namespace NugetPushIssueRepro.StaticAssets
{
    internal interface IStaticAssetManifestSerializer
    {
         string SerializeManifest(StaticAssetManifest manifest);
    }
}