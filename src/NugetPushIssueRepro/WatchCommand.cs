using System;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace NugetPushIssueRepro
{
    internal class WatchCommand : CliCommand
    {
        private LaunchCommand.LaunchOptionsGroup _launchOptions;

        internal override string CommandName => "watch";

        internal override string Description => "Launch and watch local project (command line and general IDE usage)";

        protected override void CreateOptions(CommandLineApplication command)
        {
            _launchOptions = new LaunchCommand.LaunchOptionsGroup(command);
        }

        protected override async Task<int> Run(MarvelMicroserviceConfig config)
        {
            var launchOptionsResult = await _launchOptions.ProcessOptions();
            if (!launchOptionsResult.AreValid)
            {
                return 1;
            }

            var exitCode = await LaunchCommand.Launch(config, launchOptionsResult.Value);
            if (exitCode != 0)
            {
                return exitCode;
            }

            var containerName = config.DevContainerName;

            var container = await DockerCommands.GetContainerByName(containerName);
            if (container?.IsRunning != true)
            {
                Output.Info($"Could not find running container {containerName}");
                return 1;
            }

            // Call basic command to see if we're executing in a TTY
            var ttyTest = await CommandUtilities.RunCommandAsync("docker", $"exec -it {container.ContainerId} /bin/bash -c echo hi", throwOnErrorExitCode: false);
            var isTty = true;
            if (ttyTest.ExitCode != 0)
            {
                var stdErr = string.Join("\n", ttyTest.StandardError);
                if (!stdErr.Contains("input device is not a TTY"))
                {
                    throw new Exception($"Unexpected exception encounterd checking for TTY StandardError={stdErr}");
                }
                isTty = false;
            }

            Output.Info($"Attaching to container {containerName}");
            // Use TTY option when available so that Ctrl+C in the terminal kills the process inside the container as well. Option cannot be used during integration test runs.
            var ttyArg = isTty ? "t" : "";
            exitCode = CommandUtilities.ExecuteCommand("docker", $"exec -i{ttyArg} {container.ContainerId} /bin/bash {ContainerPaths.DockerTaskScriptPath} watchAndRun");

            return exitCode;
        }
    }
}
