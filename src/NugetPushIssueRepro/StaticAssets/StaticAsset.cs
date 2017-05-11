namespace NugetPushIssueRepro.StaticAssets
{
    internal class StaticAsset
    {
        // Properties marked internal are used internally but not serialized as part of the manifest file output

        internal string FullPath { get; set; }
        internal string RelativePath { get; set; }
        internal string Key { get; set; }
        public string Hash { get; set; }
        public string Url { get; set; }
    }
}