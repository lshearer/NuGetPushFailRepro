using System;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace NugetPushIssueRepro
{
    internal abstract class CliCommand
    {

        protected const string ProjectNameEnvVariable = "MARVEL_PROJECTNAME";
        protected const string SolutionNameEnvVariable = "MARVEL_SOLUTIONNAME";
        protected const string WebappDirEnvVariable = "MARVEL_WEBAPP_DIR";
        protected const string BrowserAppDirEnvVariable = "MARVEL_BROWSER_APP_DIR";
        internal abstract string CommandName { get; }

        internal abstract string Description { get; }

        protected virtual void CreateOptions(CommandLineApplication command)
        {
            // Override to create command options and store local references to the return values.
            // E.g.,
            // this._verboseOption = command.Option("-v | --verbose", "Use verbose output", CommandOptionType.NoValue);

        }
        protected abstract Task<int> Run(MarvelMicroserviceConfig config);

        public void Register(CommandLineApplication app, string currentDirectory, CommandOption verboseOption)
        {
            app.Command(CommandName, command =>
            {
                command.Description = Description;

                AddHelpOption(command);
                CreateOptions(command);
                var commandVerboseOption = AddVerboseOption(command);

                command.OnExecute(() =>
                {
                    // Allow `-v` to be added before or after the subcommand, i.e., `dotnet marvel -v build` or `dotnet marvel build -v`
                    Output.UseVerbose = verboseOption.HasValue() || commandVerboseOption.HasValue();

                    var config = new MarvelMicroserviceConfig(currentDirectory);
                    Output.Info($"Running `dotnet {Program.CliCommandName} {CommandName}`...");
                    Output.Verbose("Using verbose logging");
                    Output.Info($"Using solution at {config.BaseDirectory}");

                    TrySetTestAuthentication();
                    var task = Task.Run(async () => await Run(config));
                    var code = task.GetAwaiter().GetResult();

                    if (code == 0)
                    {
                        Output.Success("Success.");
                    }
                    else
                    {
                        Output.Error("Failed.");
                    }
                    return code;
                });
            });
        }

        internal static class TestEnvironmentVariables
        {
            public const string AwsAccessKeyId = "MARVEL_TOOLS_INTEGRATION_TEST_ACCESS_KEY_ID";
            public const string AwsSecretAccessKey = "MARVEL_TOOLS_INTEGRATION_TEST_SECRET_ACCESS_KEY";
        }

        private void TrySetTestAuthentication()
        {
            var accessKeyId = Environment.GetEnvironmentVariable(TestEnvironmentVariables.AwsAccessKeyId);
            var secretAccessKey = Environment.GetEnvironmentVariable(TestEnvironmentVariables.AwsSecretAccessKey);
            if (!string.IsNullOrWhiteSpace(accessKeyId) && !string.IsNullOrWhiteSpace(secretAccessKey))
            {
                Output.Info("Setting AWS credentials from environment variables");
                Security.UseAwsCredentials(accessKeyId, secretAccessKey);
            }
        }

        internal static CommandOption AddHelpOption(CommandLineApplication command)
        {
            return command.HelpOption("-h | -? | --help");
        }

        internal static CommandOption AddVerboseOption(CommandLineApplication command)
        {
            return command.Option("-v | --verbose", "Use verbose logging", CommandOptionType.NoValue);
        }

    }
}
