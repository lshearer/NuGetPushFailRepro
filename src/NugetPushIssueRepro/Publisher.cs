using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NugetPushIssueRepro.StaticAssets;

namespace NugetPushIssueRepro
{
    internal class Publisher
    {
        internal static async Task<int> Publish(MarvelMicroserviceConfig config, IBuildConfigurationBuilder configBuilder, IConfigurationFileMerger configMerger, IConfigurationFileUploader configUploader, IConfigurationFileValidator configValidator, IStaticAssetProcessor staticAssetProcessor, string nugetApiKey, string awsAccessKey, string awsAccessSecret, string branch, string gitSha, string buildNumber, bool mergeAndUploadServiceConfig, bool mergeServiceConfig)
        {
            Security.UseAwsCredentials(awsAccessKey, awsAccessSecret);

            var publishImage = ImageNameBuilder.CreateImageNameAndTag(
                config.ServiceName,
                branch,
                gitSha,
                DateTime.UtcNow,
                buildNumber);

            string[] serviceConfigFiles = null;
            if (mergeAndUploadServiceConfig || mergeServiceConfig)
            {
                GenerateBuildFile(configBuilder, config.BuildConfigFilePath, gitSha, branch, publishImage.FullPath, buildNumber);
                serviceConfigFiles = await MergeAllServiceConfigFiles(configMerger, config.SourceDirectory, config.ServiceConfigFileName, config.BuildConfigFilePath);
                var configIsValid = await ValidateAllServiceConfigFiles(configValidator, config.SourceDirectory, serviceConfigFiles);
                if (!configIsValid)
                {
                    Output.Error("Invalid service configuration.");
                    return 1;
                }
            }

            var exitCode = await BuildCommand.Build(config, publishImage.FullPath, new BuildConfig
            {
                BranchName = branch,
                BuildNumber = buildNumber,
            }, staticAssetProcessor);

            if (exitCode != 0)
            {
                return exitCode;
            }

            try
            {
                exitCode = PublishClientPackage(config, nugetApiKey, awsAccessKey, awsAccessSecret, branch, gitSha, buildNumber);
                if (exitCode != 0)
                {
                    return exitCode;
                }

                // Publish to ECR
                Output.Info($"Publishing {publishImage.FullPath}");
                await Security.EnsureAuthenticatedWithEcr();

                exitCode = CommandUtilities.ExecuteCommand("docker", $"push {publishImage.FullPath}");
                if (exitCode != 0)
                {
                    return exitCode;
                }
            }
            finally
            {
                // TODO always remove image, even on publish failure
                await CommandUtilities.RunCommandAsync("docker", $"rmi {publishImage.FullPath}", errorMessage: $"Failed to remove image {publishImage.FullPath}.");
                Output.Info($"Removed local image {publishImage.FullPath}");
            }

            try
            {
                if (mergeAndUploadServiceConfig && serviceConfigFiles != null)
                {
                    await UploadAllServiceConfigFiles(configUploader, config.SourceDirectory, serviceConfigFiles, publishImage.Tag);
                }
            }
            catch (Exception ex)
            {
                Output.Error($"Unable to upload service configuration files. Error: {ex.Message}");
                return 1;
            }

            File.WriteAllText(Path.Combine(config.WebappDirectory, "PublishedImageUrl.txt"), publishImage.FullPath);
            Output.Info("Publish successful");
            return 0;
        }

        private static int PublishClientPackage(MarvelMicroserviceConfig config, string nugetApiKey, string awsAccessKey, string awsAccessSecret, string branch, string gitSha, string buildNumber)
        {
            var clientDir = config.ClientPackageDirectory;
            if (!Directory.Exists(clientDir))
            {
                Output.Info($"No client package directory found at {clientDir}.");
                Output.Info($"Skipping client package publish.");
                return 0;
            }
            Output.Info("Publishing Client NuGet package.");
            var exitCode = CommandUtilities.ExecuteCommand("dotnet", $"restore", workingDirectory: clientDir);
            if (exitCode != 0)
            {
                return exitCode;
            }

            exitCode = CommandUtilities.ExecuteCommand("dotnet", $"library publish " +
                $"-b {branch} " +
                $"-k {nugetApiKey} " +
                $"-a {awsAccessKey} " +
                $"-s {awsAccessSecret} " +
                $"-n {buildNumber} " +
                $"--tests-optional", workingDirectory: clientDir);
            if (exitCode != 0)
            {
                return exitCode;
            }

            return 0;
        }

        internal static async Task<string[]> MergeAllServiceConfigFiles(IConfigurationFileMerger configFileMerger, string sourcePath, string serviceConfigFileName, string buildConfigFile)
        {
            var buildConfigRelativePath = buildConfigFile.Replace(sourcePath, "");
            var serviceConfigFilePath = Path.Combine(sourcePath, serviceConfigFileName);
            if (!File.Exists(serviceConfigFilePath))
            {
                throw new FileNotFoundException($"The service configuration file doesn't exist at ({serviceConfigFilePath}). Is this a marvel service?");
            }
            // Get All EnvironmentFiles
            var environmentFiles = System.IO.Directory.GetFiles(sourcePath, "environment-*.yaml");
            var environmentConfigFileNames = environmentFiles.Select(environmentFile => Path.GetFileName(environmentFile));

            // Merge EnvironmentFiles with ServiceFiles
            var mergeEnvironmentTasks = environmentConfigFileNames.Select(environmentConfigFileName => MergeConfigFiles(configFileMerger, sourcePath, serviceConfigFileName, environmentConfigFileName));
            var mergedServiceEnvironmentFiles = await Task.WhenAll(mergeEnvironmentTasks);

            // Merge Environment+ServiceFiles with BuildFiles
            var mergeBuildTasks = mergedServiceEnvironmentFiles.Select(mergedServiceEnvironmentFile => MergeConfigFiles(configFileMerger, sourcePath, mergedServiceEnvironmentFile, buildConfigRelativePath));
            var finalConfigFiles = await Task.WhenAll(mergeBuildTasks);

            return finalConfigFiles;
        }

        private static void GenerateBuildFile(IBuildConfigurationBuilder configBuilder, string buildConfigFilePath, string commit, string branch, string containerImage, string buildNumber)
        {
            configBuilder.WriteBuildConfigurationToFile(commit, branch, containerImage, buildNumber, buildConfigFilePath);
        }
        internal static async Task<string> MergeConfigFiles(IConfigurationFileMerger configFileMerger, string sourcePath, string DefaultConfigFile, string OverridesConfigFile)
        {
            var DefaultConfigFileNoExtension = Path.GetFileNameWithoutExtension(DefaultConfigFile);
            var OverridesConfigFileNoExtension = Path.GetFileNameWithoutExtension(OverridesConfigFile);
            var generatedResultFileName = $"merged-{DefaultConfigFileNoExtension}-{OverridesConfigFileNoExtension}.yaml";
            await configFileMerger.MergeConfigurationFilesAsync(sourcePath, DefaultConfigFile, OverridesConfigFile, generatedResultFileName);
            return generatedResultFileName;
        }

        private static async Task<bool> ValidateServiceConfigFile(IConfigurationFileValidator configValidator, string sourcePath, string completeConfigFile)
        {
            var result = await configValidator.ValidateConfigurationFile(sourcePath, completeConfigFile);
            if (!result.IsValid)
            {
                Output.Error($"Invalid Service Configuration {completeConfigFile}: {result.Response}");
            }

            return result.IsValid;
        }

        internal static async Task<bool> ValidateAllServiceConfigFiles(IConfigurationFileValidator configValidator, string sourcePath, string[] completeConfigFiles)
        {
            var validateConfigTasks = completeConfigFiles.Select(completeConfigFile => ValidateServiceConfigFile(configValidator, sourcePath, completeConfigFile));
            var results = await Task.WhenAll(validateConfigTasks);
            return results.All(r => r == true);
        }

        internal async static Task UploadAllServiceConfigFiles(IConfigurationFileUploader uploader, string sourcePath, string[] serviceConfigFiles, string s3FileName)
        {
            var uploadTasks = serviceConfigFiles.Select(serviceConfigFile => UploadServiceConfigFile(uploader, File.ReadAllText(Path.Combine(sourcePath, serviceConfigFile)), s3FileName));
            await Task.WhenAll(uploadTasks);
        }

        private async static Task UploadServiceConfigFile(IConfigurationFileUploader uploader, string configContents, string s3FileName)
        {
            // For each Service+Environment+Build file
            //   Upload to environment s3 folder.
            var s3FilePath = uploader.GenerateDestinationPath(configContents, s3FileName);
            await uploader.UploadConfiguration(configContents, s3FilePath);
        }
    }

}
