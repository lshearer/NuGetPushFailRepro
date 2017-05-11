using System.IO;
namespace NugetPushIssueRepro
{
    internal interface IBuildConfigurationBuilder
    {
        void WriteBuildConfigurationToFile(string commit, string branch, string containerImage, string buildNumber, string configFilePathname);
        void WriteBuildConfiguration(string commit, string branch, string containerImage, string buildNumber, TextWriter textWriter);
    }
}