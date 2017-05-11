namespace NugetPushIssueRepro
{
    internal class ImageNameBuilderResult
    {
        public ImageNameBuilderResult(string repository, string imageName, string tag)
        {
            Repository = repository;
            ImageName = imageName;
            Tag = tag;
        }
        public string Repository { get; set; }
        public string Tag { get; set; }
        public string ImageName { get; set; }
        public string FullPath
        {
            get
            {
                return $"{Repository}/{ImageName}:{Tag}";
            }
        }
    }
}