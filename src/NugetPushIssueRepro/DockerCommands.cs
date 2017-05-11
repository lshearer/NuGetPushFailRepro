using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NugetPushIssueRepro.Utility;

namespace NugetPushIssueRepro
{
    internal static class DockerCommands
    {
        public class ContainerInfo
        {
            public bool IsRunning { get; set; }
            public string ContainerId { get; set; }
            // public string ImageId { get; set; }
            public DateTime CreatedAt { get; set; }
            public Dictionary<string, string> Labels { get; set; }
        }

        private static async Task<List<ContainerInfo>> GetContainers(string filter = null)
        {
            await EnsureDockerIsRunning();

            var format = "\"{{.ID}}\t{{.Status}}\t{{.Image}}\t{{.CreatedAt}}\t{{.Labels}}\"";
            var filterArg = string.IsNullOrWhiteSpace(filter) ? "" : $" -f \"{filter}\" ";
            var arguments = $"ps -a {filterArg}--format {format}";
            var results = await CommandUtilities.RunCommandAsync("docker", arguments, errorMessage: "Failed to list containers.");

            var containers = results.StandardOutput
                .Where(outputLine => !string.IsNullOrWhiteSpace(outputLine))
                .Select(outputLine =>
                {
                    var split = outputLine.Split('\t');

                    return new ContainerInfo
                    {
                        ContainerId = split[0],
                        IsRunning = split[1].StartsWith("Up"),
                        // ImageId = split[2],
                        CreatedAt = ParseDateString(split[3]),
                        Labels = ParseLabels(split[4]),
                    };
                });
            return containers.ToList();
        }

        internal static Dictionary<string, string> ParseLabels(string labelString)
        {
            var labels = (labelString ?? "")
                .Split(',')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(labelPair => labelPair.Split('='))
                .ToDictionary(split => split[0], split => split[1]);

            return labels;
        }

        public static async Task<ContainerInfo> GetContainerByName(string containerName)
        {
            return (await GetContainers($"name={containerName}")).FirstOrDefault();
        }

        public static async Task<List<ContainerInfo>> GetContainersByName(string containerName)
        {
            return await GetContainers($"name={containerName}");
        }

        // private static async Task<ContainerInfo> GetContainerById(string containerId)
        // {
        //     return await GetContainer($"id={containerId}");
        // }

        public static async Task<ContainerInfo> CreateContainer(string arguments)
        {
            await EnsureDockerIsRunning();

            var create = await CommandUtilities.RunCommandAsync("docker", $"create -i {arguments}");
            var containerId = create.StandardOutput.FirstOrDefault();
            return new ContainerInfo
            {
                ContainerId = containerId,
                IsRunning = false,
            };
        }

        public static async Task StopContainer(ContainerInfo container)
        {
            await EnsureDockerIsRunning();

            await CommandUtilities.RunCommandAsync("docker", $"stop {container.ContainerId}", errorMessage: "Failed to stop container.");
        }

        public static async Task RemoveContainer(ContainerInfo container)
        {
            await EnsureDockerIsRunning();

            await CommandUtilities.RunCommandAsync("docker", $"rm -f {container.ContainerId}", errorMessage: "Failed to remove container.");
        }

        public static async Task PullImage(string imageUri)
        {
            await EnsureDockerIsRunning();

            await CommandUtilities.RunCommandAsync("docker", $"pull {imageUri}", errorMessage: $"Failed to pull image {imageUri}.");
        }

        public class ImageInfo
        {
            public string ImageId { get; set; }
            public string Repository { get; set; }
            public DateTime CreatedAt { get; set; }
        }


        private const string ImageInfoFormat = "\"{{.ID}}\t{{.Repository}}\t{{.CreatedAt}}\"";

        private static ImageInfo ParseImageInfo(string outputLine)
        {
            var split = outputLine.Split('\t');
            return new ImageInfo
            {
                ImageId = split[0],
                Repository = split[1],
                CreatedAt = ParseDateString(split[2]),
            };
        }

        // public static async Task<ImageInfo> GetImageInfoByImageId(string imageId)
        // {
        //     var result = await ProcessEx.RunAsync("docker", $"images --format {ImageInfoFormat}");
        //     if (result.ExitCode != 0)
        //     {
        //         throw new Exception($"Failed to list images.");
        //     }

        //     // This can return multiple matches of a single image ID with varying repository:tag, but the CreatedAt field will be the same.
        //     var matchingImage = result.StandardOutput.Select(ParseImageInfo)
        //         .Where(image => image.ImageId == imageId)
        //         .FirstOrDefault();

        //     if (matchingImage == null)
        //     {
        //         throw new Exception($"No image found matching id `{imageId}`");
        //     }
        //     return matchingImage;
        // }

        public static async Task<ImageInfo> GetImageInfoByName(string imageName)
        {
            await EnsureDockerIsRunning();

            var result = await CommandUtilities.RunCommandAsync("docker", $"images {imageName} --format {ImageInfoFormat}", errorMessage: $"Failed to list images for image name {imageName}.");

            // This can return multiple matches of a single image ID with varying repository:tag, but the CreatedAt field will be the same.
            var image = result.StandardOutput.Select(ParseImageInfo)
                .FirstOrDefault();

            if (image == null)
            {
                throw new Exception($"No image found matching name `{imageName}`");
            }
            return image;
        }



        internal static DateTime ParseDateString(string date)
        {
            // Strip last non digit timezones
            var stringTimezoneIndex = date.LastIndexOf(' ');
            return DateTime.Parse(date.Substring(0, stringTimezoneIndex));
        }

        public class GetOrCreateContainerResult
        {
            public ContainerInfo Container { get; set; }
            public bool IsNewContainer { get; set; }
        }

        public static async Task<GetOrCreateContainerResult> GetOrCreateContainerWithName(string containerName, string imageName, List<string> options, string commandAndArgs)
        {
            const string argsHashLabelName = "createArgumentsHash";

            var createArguments = options
                .Append($"--name {containerName}")
                .Append(imageName)
                .Append(commandAndArgs)
                .ToList();

            // Hash label argument is not part of the hash itself
            var argsHash = Hash(string.Join(" ", createArguments));
            var createArgs = string.Join(" ", createArguments.Prepend($"-l {argsHashLabelName}={argsHash}"));


            // var createArguments = $"--name {containerName} -p 5000:5000 -e \"{ProjectNameEnvVariable}={config.ProjectName}\" -v {config.BaseDirectory}:{ContainerPaths.MountedSourceDirectory} {buildImage} /bin/bash {dockerTaskScriptPath} hang";
            // var createArguments = $"--name {containerName} -p 5000:5000 -e \"{ProjectNameEnvVariable}={config.ProjectName}\" -l creationArgsSignature={argsHashPlaceholder} -l another=hey -v {config.BaseDirectory}:{ContainerPaths.MountedSourceDirectory} {buildImage} /bin/bash {dockerTaskScriptPath} hang";

            // # Ensure build image has been built locally (temporary until image is published)
            // buildImageCount=$(docker images | grep "$buildImage" -c)
            // if [[ "$buildImageCount" -eq "0" ]]; then
            //     echo "Building base image "
            //     cd ../marvel-build-container/dev/
            //     ./build.sh
            //     cd "$dir"
            // fi

            var container = await GetContainerByName(containerName);
            var isNewContainer = false;

            // This is (primarily?) for development of the tools package. Prevents needing to manually remove the containers after building a new base image or change in arguments.
            if (container != null)
            {
                if (await IsContainerUpToDateWithImage(container, imageName))
                {
                    string containerHash;
                    if (container.Labels.TryGetValue(argsHashLabelName, out containerHash) && containerHash == argsHash)
                    {
                        Output.Info($"Container is up to date with latest base image and arguments");
                    }
                    else
                    {
                        Output.Info($"Removing container created with outdated arguments");
                        await RemoveContainer(container);
                        container = null;
                    }
                }
                else
                {
                    Output.Info($"Removing container created from outdated image.");
                    await RemoveContainer(container);
                    container = null;
                }
            }

            if (container == null)
            {
                Output.Info($"Creating new container {containerName}");

                container = await CreateContainer(createArgs);
                isNewContainer = true;
            }

            return new GetOrCreateContainerResult
            {
                Container = container,
                IsNewContainer = isNewContainer,
            };
        }

        internal static string Hash(string input)
        {
            return WebUtility.UrlEncode(input);
            // Was using a hash instead, but currently failing to load Crypto library when loaded as a dotnet tool via NuGet package.
            // using (SHA256 sha1 = SHA256.Create())
            // {
            //     var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            //     var sb = new StringBuilder(hash.Length * 2);

            //     foreach (byte b in hash)
            //     {
            //         // can be "x2" if you want lowercase
            //         sb.Append(b.ToString("x2"));
            //     }

            //     return sb.ToString();
            // }
        }

        private static async Task<bool> IsContainerUpToDateWithImage(DockerCommands.ContainerInfo container, string imageName)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }
            var imageInfo = await DockerCommands.GetImageInfoByName(imageName);
            return container.CreatedAt >= imageInfo.CreatedAt;
        }

        public static string GetLogsForContainerCommand(ContainerInfo container)
        {
            return $"`docker logs {container.ContainerId}`";
        }

        private static async Task EnsureDockerIsInstalled()
        {
            try
            {
                await CommandUtilities.RunCommandAsync("docker", "-v", throwOnErrorExitCode: true);
            }
            catch (Exception e)
            {
                throw new Exception("Docker is not found. Docker is required to run integration tests.", e);
            }
        }

        internal static async Task EnsureDockerIsRunning()
        {
            await EnsureDockerIsInstalled();
            var result = await CommandUtilities.RunCommandAsync("docker", "ps", throwOnErrorExitCode: false);
            if (result.ExitCode == 0)
            {
                return;
            }

            if (result.StandardError.Any(line => line.Contains("Cannot connect to the Docker daemon")))
            {
                throw new Exception("Failed to connect to Docker. Ensure Docker is running. If Docker is running, it may need restarted.");
            }
            else
            {
                throw new Exception("Unexpected error connecting to Docker. If Docker is running, it may need restarted.");
            }
        }
    }
}