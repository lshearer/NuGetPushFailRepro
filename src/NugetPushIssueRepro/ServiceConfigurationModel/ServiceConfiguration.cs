using YamlDotNet.Serialization;

namespace NugetPushIssueRepro.ServiceConfigurationModel
{
    internal class ServiceConfigFile
    {
        [YamlMember(Alias = "service")]
        public ServiceConfigSection Service { get; set; }

        [YamlMember(Alias = "environment")]
        public string Environment { get; set; }

        [YamlMember(Alias = "build")]
        public BuildConfigSection Build { get; set; }

        [YamlMember(Alias = "infrastructure")]
        public InfrastructureSection Infrastructure { get; set; }
    }
}
