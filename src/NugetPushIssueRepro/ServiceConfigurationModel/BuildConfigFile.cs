using YamlDotNet.Serialization;

namespace NugetPushIssueRepro.ServiceConfigurationModel
{
    internal class BuildConfigFile
    {
        public BuildConfigFile()
        {
        }
        public BuildConfigFile(string headCommit, string containerImage, string branch, string buildNumber)
        {
            Build = new BuildConfigSection();
            Build.HeadCommit = headCommit;
            Build.DockerImage = containerImage;
            Build.Branch = branch;
            Build.BuildNumber = buildNumber;
        }
        [YamlMember(Alias = "build")]
        public BuildConfigSection Build { get; set; }
    }
}