using YamlDotNet.Serialization;

namespace NugetPushIssueRepro.ServiceConfigurationModel
{
    internal class ServiceConfigSection
    {
        [YamlMember(Alias = "name")]
        public string Name { get; set; }
    }
}