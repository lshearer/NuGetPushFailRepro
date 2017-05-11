using YamlDotNet.Serialization;

namespace NugetPushIssueRepro.ServiceConfigurationModel
{
    internal class BuildConfigSection
    {
        [YamlMember(Alias = "head_commit")]
        public string HeadCommit { get; set; }

        [YamlMember(Alias = "docker_image")]
        public string DockerImage { get; set; }

        [YamlMember(Alias = "branch")]
        public string Branch { get; set; }

        [YamlMember(Alias = "build_number")]
        public string BuildNumber { get; set; }
    }
}