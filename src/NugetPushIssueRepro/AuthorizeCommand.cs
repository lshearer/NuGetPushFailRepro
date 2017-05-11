using System.Threading.Tasks;

namespace NugetPushIssueRepro
{
    /// <summary>
    /// Authorization command to allow for storing AWS credentials. This is seprate from the inline prompts to allow for
    /// cases where other commands are run as non-interactive tasks within an editor. This simplifies the troubleshooting steps
    /// by directing users to just run this specific command, then re-trying whatever they were doing before (e.g., pressing F5 in VS code).
    /// </summary>
    internal class AuthorizeCommand : CliCommand
    {
        internal override string CommandName => "auth";

        internal override string Description => "Set AWS credentials for accessing private Docker images in ECR";

        protected override async Task<int> Run(MarvelMicroserviceConfig config)
        {
            var authenticated = await Security.EnsureAuthenticatedWithEcr();

            if (authenticated)
            {
                Output.Info("Authorization was successful.");
                return 0;
            }
            else
            {
                Output.Error("Authorization failed.");
                return 1;
            }
        }
    }
}
