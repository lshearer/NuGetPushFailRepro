using System.Threading.Tasks;

namespace NugetPushIssueRepro
{
    internal class DebugCommand : CliCommand
    {
        internal override string CommandName => "debug";

        internal override string Description => "Connect to running container for debugging and watching app (VS Code usage).";

        protected override async Task<int> Run(MarvelMicroserviceConfig config)
        {
            var exitCode = 0;

            var containerName = config.DevContainerName;

            var container = await DockerCommands.GetContainerByName(containerName);
            if (container?.IsRunning != true)
            {
                Output.Info($"Could not find running container {containerName}");
                return 1;
            }

            Output.Info($"Attaching to container {containerName}");

            exitCode = CommandUtilities.ExecuteCommand("docker", $"exec -i {container.ContainerId} /bin/bash {ContainerPaths.DockerTaskScriptPath} watchAndDebug");

            return exitCode;
        }
    }
}
