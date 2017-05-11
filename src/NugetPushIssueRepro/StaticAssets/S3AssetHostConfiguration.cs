namespace NugetPushIssueRepro.StaticAssets
{
    internal class S3AssetHostConfiguration
    {
        // Adding this to be explicit here. This could be made configurable, but doesn't currently need to be.
        public Amazon.RegionEndpoint AwsRegion => Amazon.RegionEndpoint.USEast1;
        public string BucketName { get; set; }
        public string PublicHostName { get; set; }

        public static S3AssetHostConfiguration TestAssets = new S3AssetHostConfiguration
        {
            BucketName = "",
            PublicHostName = "",
        };

        public static S3AssetHostConfiguration ProductionAssets = new S3AssetHostConfiguration
        {
            BucketName = "",
            PublicHostName = "",
        };
    }
}