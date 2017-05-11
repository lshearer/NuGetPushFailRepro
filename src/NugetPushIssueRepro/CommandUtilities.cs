using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RunProcessAsTask;

namespace NugetPushIssueRepro
{
    internal static class CommandUtilities
    {
        internal static int ExecuteCommand(string executable, string args, bool writeOutput = true, string workingDirectory = "")
        {
            var command = $"{executable} {args}";
            if (writeOutput)
            {
                Output.CommandExecution(command);
            }

            var consoleColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Gray;

            Process p;
            try
            {
                p = Process.Start(new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = args,
                    RedirectStandardError = !writeOutput,
                    RedirectStandardOutput = !writeOutput,
                    WorkingDirectory = workingDirectory,
                });
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to start command `{command}`", e);
            }

            p.WaitForExit();

            Console.ForegroundColor = consoleColour;

            if (writeOutput)
            {
                Output.ExitCode(executable, p.ExitCode);
            }

            if (p.ExitCode != 0 && writeOutput)
            {
                Output.Error($"`{command}` exited with code {p.ExitCode}");
            }

            return p.ExitCode;
        }

        internal static async Task<ProcessResults> RunCommandAsync(
            string filename,
            string arguments,
            string workingDirectory = null,
            bool throwOnErrorExitCode = true,
            string errorMessage = null,
            CancellationToken? token = null)
        {
            // Output.Info($"Executing async command >{filename} {arguments}");
            var results = await ProcessEx.RunAsync(filename, arguments, workingDirectory);
            if (results.ExitCode != 0 && throwOnErrorExitCode)
            {
                var commandLog = $"Command failed with exit code {results.ExitCode}: > {filename} {arguments}";
                if (token?.IsCancellationRequested != true)
                {
                    if (!string.IsNullOrWhiteSpace(errorMessage))
                    {
                        Output.Error(errorMessage);
                    }
                    Output.Error(commandLog);
                    var indent = "==>";
                    Output.Error($"StandardError:\n{indent}{string.Join($"\n{indent}", results.StandardError)}");
                    Output.Verbose($"StandardOutput:\n{indent}{string.Join($"\n{indent}", results.StandardOutput)}");
                }
                throw new Exception(commandLog);
            }
            return results;
        }
    }
}
