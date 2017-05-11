using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace NugetPushIssueRepro
{
    internal class HSDConfigurationFileMerger : IConfigurationFileMerger
    {
        public async Task MergeConfigurationFilesAsync(string baseDirectory, string defaultPath, string overridesPath, string outputPath)
        {
            var buildImageUri = await EcrResources.HsdImageUrl.EnsureImageIsPulled();
            if (!buildImageUri.WasSuccessful)
            {
                throw new Exception($"Unable to get image {buildImageUri.Value} from ECR");
            }

            var dockerRunOptions = new List<string>
            {
                $"--rm",
                $"-v {baseDirectory}:{ContainerPaths.MountedSourceDirectory}"
            };

            var hsdMergeCommand = $"/bin/bash hsd-merge.sh {ContainerPaths.MountedSourceDirectory}/{defaultPath} {ContainerPaths.MountedSourceDirectory}/{overridesPath} {ContainerPaths.MountedSourceDirectory}/{outputPath}";
            var exitCode = CommandUtilities.ExecuteCommand("docker", $"run {string.Join(" ", dockerRunOptions)} {buildImageUri.Value} {hsdMergeCommand}");
            if (exitCode != 0)
            {
                throw new Exception("HSD merge failed.");
            }
        }
    }
}