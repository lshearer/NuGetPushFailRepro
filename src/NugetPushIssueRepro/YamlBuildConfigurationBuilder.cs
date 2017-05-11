using System.IO;
using YamlDotNet.Serialization;
using NugetPushIssueRepro.ServiceConfigurationModel;

namespace NugetPushIssueRepro
{
    internal class YamlBuildConfigurationBuilder : IBuildConfigurationBuilder
    {
        public void WriteBuildConfiguration(string commit, string branch, string containerImage, string buildNumber, TextWriter textWriter)
        {
            var fileRoot = new BuildConfigFile(commit, containerImage, branch, buildNumber);
            var serializer = new Serializer();
            serializer.Serialize(textWriter, fileRoot);
        }

        public void WriteBuildConfigurationToFile(string commit, string branch, string containerImage, string buildNumber, string configFilePathname)
        {
            using (TextWriter fileWriter = File.CreateText(configFilePathname))
            {
                WriteBuildConfiguration(commit, branch, containerImage, buildNumber, fileWriter);
            }
        }
    }
}