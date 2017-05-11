using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NugetPushIssueRepro.StaticAssets;
using NugetPushIssueRepro.Utility;

namespace NugetPushIssueRepro
{
    internal class BuildCommand : CliCommand
    {
        internal override string CommandName => "build";

        internal override string Description => "Build microservice docker image for distribution.";

        private readonly IStaticAssetProcessor _staticAssetProcessor;

        private readonly IAccessor<S3AssetHostConfiguration> _assetHostConfigurationAccessor;

        public BuildCommand(IStaticAssetProcessor staticAssetProcessor, IAccessor<S3AssetHostConfiguration> assetHostConfigurationAccessor)
        {
            _staticAssetProcessor = staticAssetProcessor.ThrowIfNull(nameof(staticAssetProcessor));
            _assetHostConfigurationAccessor = assetHostConfigurationAccessor.ThrowIfNull(nameof(assetHostConfigurationAccessor));
        }

        protected override Task<int> Run(MarvelMicroserviceConfig config)
        {
            // Used for image labeling purposes, so shouldn't really matter on local build command
            var devBuildConfig = new BuildConfig
            {
                BranchName = "local-dev-build",
                BuildNumber = "123456",
            };

            // Use short-term storage for `build` command because it is only used locally. `publish` is used on TeamCity.
            _assetHostConfigurationAccessor.SetValue(S3AssetHostConfiguration.TestAssets);
            return Build(config, config.DevPublishedRuntimeImageName, devBuildConfig, _staticAssetProcessor);
        }

        public static async Task<int> Build(MarvelMicroserviceConfig config, string publishedRuntimeImageName, BuildConfig buildConfig, IStaticAssetProcessor staticAssetProcessor)
        {
            var buildImageUri = await EcrResources.DotnetMicroserviceBuildImageUrl.EnsureImageIsPulled();
            if (!buildImageUri.WasSuccessful)
            {
                return 1;
            }

            var taskTimer = Stopwatch.StartNew();

            // Clear the old temp dir to ensure a fresh publish if running locally
            var outputPath = config.DotnetPublishOutputPath;
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }
            Directory.CreateDirectory(outputPath);

            var dockerRunOptions = new List<string>
            {
                $"--rm",
                $"-e \"{LaunchCommand.ProjectNameEnvVariable}={config.ProjectName}\"",
                $"-e \"{LaunchCommand.SolutionNameEnvVariable}={config.SolutionName}\"",
                $"-e \"{WebappDirEnvVariable}={ContainerPaths.GetWebappDirectory(config)}\"",
                $"-e \"{BrowserAppDirEnvVariable}={ContainerPaths.GetBrowserAppDirectory(config)}\"",
                $"-v {config.BaseDirectory}:{ContainerPaths.MountedSourceDirectory}",
                $"-v {outputPath}:{ContainerPaths.BuildOutputDirectory}",
            };

            var runtimeImageLabelsTask = new LabelBuilder(config).GetLabels(buildConfig);

            var exitCode = 0;

            var dotnetBuildTimer = Stopwatch.StartNew();
            Output.Info($"Building dotnet webapp.");

            // Run container to build app and copy published resources to mounted output directory
            exitCode = CommandUtilities.ExecuteCommand("docker", $"run {string.Join(" ", dockerRunOptions)} {buildImageUri.Value} /bin/bash {ContainerPaths.DockerTaskScriptPath} buildWithoutCompose");
            if (exitCode != 0)
            {
                return exitCode;
            }
            Output.Info($"dotnet webapp build completed {dotnetBuildTimer.Elapsed}");

            // Run static asset build from output directory
            await staticAssetProcessor.ProcessStaticAssets(Path.Combine(config.DotnetPublishOutputPath, "wwwroot"));

            // Build the image from the source output
            var dockerBuildTimer = Stopwatch.StartNew();
            Output.Info($"Building docker image {publishedRuntimeImageName}.");

            var labelArgs = LabelUtilities.FormatLabelsAsArguments(await runtimeImageLabelsTask);

            var buildArgs = labelArgs.Concat(new List<string>{
                $"-t {publishedRuntimeImageName}",
                $"--build-arg webappAssemblyPath={config.PublishedWebappAssemblyPath}",
                config.DotnetPublishOutputPath,
            });

            exitCode = CommandUtilities.ExecuteCommand("docker", $"build {string.Join(" ", buildArgs)}");
            if (exitCode != 0)
            {
                return exitCode;
            }
            Output.Info($"Docker build completed {dockerBuildTimer.Elapsed}");

            Output.Info($"Build time elapsed {taskTimer.Elapsed}");

            return exitCode;
        }
    }
}
