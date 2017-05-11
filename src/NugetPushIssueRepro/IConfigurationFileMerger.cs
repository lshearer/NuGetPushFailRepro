using System.Threading.Tasks;
namespace NugetPushIssueRepro
{
    internal interface IConfigurationFileMerger
    {
        Task MergeConfigurationFilesAsync(string baseDirectory, string defaultPath, string overridesPath, string outputPath);
    }
}