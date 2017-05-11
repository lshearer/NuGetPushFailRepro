using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.ECR;
using Amazon.ECR.Model;
using Amazon.Runtime;
using Amazon.Util;
using Microsoft.Extensions.Configuration;

namespace NugetPushIssueRepro
{
    internal class Security
    {
        private static readonly RegionEndpoint _regionEndpoint = RegionEndpoint.USEast1;

        private static readonly TimeSpan _awsTimeout = TimeSpan.FromSeconds(10);

        private static class SecretStoreKeys
        {
            public const string AccessKeyId = "accessKeyId";
            public const string SecretKey = "secretKey";

        }

        internal static IConfigurationRoot GetConfiguration()
        {
            var builder = new ConfigurationBuilder();

            // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
            // builder.AddUserSecrets<Security>();

            return builder.Build();
        }

        private static string _accessKeyId = null;
        private static string _secretKey = null;

        internal static void UseAwsCredentials(string accessKeyId = null, string secretKey = null)
        {
            _accessKeyId = accessKeyId;
            _secretKey = secretKey;
        }

        internal static async Task<bool> EnsureAuthenticatedWithEcr()
        {
            return await AuthenticateWithEcr.Value;
        }

        internal static Lazy<Task<bool>> AuthenticateWithEcr = new Lazy<Task<bool>>(async () =>
        {
            try
            {
                var authorizationToken = await GetAuthorizationToken(_accessKeyId, _secretKey);
                if (authorizationToken == null)
                {
                    Output.Error("Unable to retrieve authorization token.");
                    return false;
                }

                return CommandUtilities.ExecuteCommand("docker", GetLoginToAwsDockerParams(authorizationToken)) == 0;
            }
            catch (AmazonClientException e)
            {
                Output.VerboseException(e);
                // This should not happen. We've checked that profile is known.
                Output.Error("Problem with getting AWS credentials from SDK Store. Error: " + e.Message);
                return false;
            }
            catch (Exception e)
            {
                Output.VerboseException(e);
                var ecrException = e.InnerException as AmazonECRException;
                if (ecrException != null)
                {
                    if (ecrException.ErrorCode == "AccessDeniedException")
                    {
                        Output.Error($"You are not authorized to get Authorization Token. Message: {ecrException.Message}");
                        return false;
                    }
                }

                var timeoutException = e.InnerException as TaskCanceledException;
                if (timeoutException != null)
                {
                    Output.Error("Timeout while trying to connect with AWS. Please try again");
                    return false;
                }

                // This should not happen. We've checked that profile is known.
                Output.Error(
                    "Unexpected error while trying to log in into AWS. Please contact #watch-ops with message: " +
                    e.Message);
                return false;
            }
        });

        private static async Task<GetAuthorizationTokenResponse> GetAuthorizationToken(string accessKeyId, string secretKey)
        {
            Output.Verbose($"Retrieving authorization token {nameof(accessKeyId)}={accessKeyId}, {nameof(secretKey)}={secretKey}.");

            GetAuthorizationTokenResponse authorizationToken;

            // We only want to get & save secrets from/to local configuration when we have not provided them
            var needToStoreSecrets = false;
            if (accessKeyId == null || secretKey == null)
            {
                Output.Verbose("Attempting to load AWS credentials from secrets store.");
                var securityConfig = GetConfiguration();
                if (accessKeyId == null)
                {
                    accessKeyId = securityConfig[SecretStoreKeys.AccessKeyId];
                }
                if (secretKey == null)
                {
                    secretKey = securityConfig[SecretStoreKeys.SecretKey];
                }
            }

            var firstPrompt = true;
            do
            {
                if (accessKeyId == null || secretKey == null)
                {
                    Output.Verbose("Attempting to prompt for AWS credentials.");

                    needToStoreSecrets = true;
                    if (!ConsoleHelper.IsInteractive)
                    {
                        // CLI is running from within a script in a non-interactive terminal, e.g., from pressing F5 in VS Code. The
                        // auth command will get users to this same code path, but in an interactive terminal.

                        var cliCommand = Program.CliCommandName;
                        var authCommand = new AuthorizeCommand().CommandName;

                        Output.Error(Output.HorizontalLine());
                        Output.Error("Your machine is missing necessary AWS credentials. Authorize your machine by opening a terminal and " +
                            "running the following commands, then try your actions again. This authorization only needs to be performed once per user/machine:");
                        Output.Error($"> cd {Directory.GetCurrentDirectory()}");
                        Output.Error($"> dotnet {cliCommand} {authCommand}");
                        Output.Error($"If you have further issues, please contact #watch-ops.");
                        Output.Error(Output.HorizontalLine());

                        return null;
                    }

                    // TODO add link to docs page for finding/creating AWS access keys and getting help from watch-ops to set up account
                    // or add permissions.
                    if (firstPrompt)
                    {
                        Output.Error("Your machine is missing necessary AWS credentials. The following authorization only needs to be performed " +
                            "once per user/machine. For assistance, please contact #watch-ops.");
                        firstPrompt = false;
                    }
                    Output.Error("Paste in your AWS ACCESS KEY ID:");
                    accessKeyId = ConsoleHelper.ReadPassword();

                    Output.Error("Now paste in your AWS SECRET ACCESS KEY:");
                    secretKey = ConsoleHelper.ReadPassword();
                }

                try
                {
                    var ecrClient = new AmazonECRClient(accessKeyId, secretKey, _regionEndpoint);

                    // TODO Potentially slow. We might need to create a cached toked since it'll be valid for 12  hours
                    // Another thing is that there is a throttling for GetAuthorizationTokenAsync (1 call / second)
                    // There's not a good way to check the stored credentials without making
                    // an actual call to ECR (e.g. a docker pull). Doing that on every operation
                    // would be relatively slow. Could we write a hidden file on successful auth
                    // check and expire it after 8 hours?
                    Output.Verbose($"Retrieving ECR authentication token");
                    authorizationToken = await ecrClient.GetAuthorizationTokenAsync(new GetAuthorizationTokenRequest(),
                        new CancellationTokenSource(_awsTimeout).Token);


                    // We have successfully got authorization token let's save our access key and secret in user-secrets.
                    // We only save them when we've not provided access and secret via parameters.
                    if (needToStoreSecrets)
                    {
                        Output.Verbose($"Storing secret {SecretStoreKeys.AccessKeyId}");
                        await SaveSecret(SecretStoreKeys.AccessKeyId, accessKeyId);
                        Output.Verbose($"Storing secret {SecretStoreKeys.SecretKey}");
                        await SaveSecret(SecretStoreKeys.SecretKey, secretKey);
                    }
                    break;
                }
                catch (Exception e)
                {
                    var amazonEcrException = e.InnerException as AmazonECRException;
                    if (amazonEcrException != null && amazonEcrException.ErrorCode.Equals("UnrecognizedClientException"))
                    {
                        Output.VerboseException(e);
                        Output.Error("Bad credentials. Please try again.");
                        accessKeyId = null;
                        secretKey = null;
                        continue;
                    }

                    // TODO handle case where permissions are inadequate (?)
                    Output.Error("Something went wrong while connecting to AWS. Please try again later.");
                    Output.Exception(e);
                    throw;
                }
            } while (true);
            return authorizationToken;
        }

        internal static async Task SaveSecret(string key, string value)
        {
            // TODO - pull secrets id from assembly info.
            // Setting ID here so that marvel-tools secrets can be separate from project-specific secrets
            // Ideally we can remove the dependency on the user secrets tools package from the app as well, and call it directly from here, but I'm not sure what that would look like yet.
            var result = await CommandUtilities.RunCommandAsync("dotnet", $"user-secrets set --id marvel-tools-bh61t9klymwu5qfohd1paq44slmg76rpur103ewo {key} {value}", throwOnErrorExitCode: false);
            if (result.ExitCode != 0)
            {
                if (result.StandardError.Any(line => line.Contains("No executable found matching command \"dotnet-user-secrets\"")))
                {
                    // TODO add example and/or additional help link
                    throw new InvalidOperationException($"Unable to save secret for \"{key}\". dotnet-user-secrets command not found. " +
                        "Ensure you have \"Microsoft.Extensions.SecretManager.Tools\": \"1.1.0-preview4-final\" defined in the \"tools\" section of your project.json " +
                        "and you've run `dotnet restore`.");
                }
                if (result.StandardError.Concat(result.StandardOutput).Any(line => line.Contains("Missing 'userSecretsId'")))
                {
                    // TODO - add exact key needed or an example, docs link, etc.
                    throw new InvalidOperationException("Missing 'userSecretsId' in project.json.");
                }

                throw new Exception($"Unable to save secret for \"{key}\". Ensure you have \"Microsoft.Extensions.SecretManager.Tools\": \"1.1.0-preview4-final\" defined in " +
                    $"the \"tools\" section of your project.json and you've run `dotnet restore`. StandardOutput={string.Join("\n", result.StandardOutput)} StandardError={string.Join("\n", result.StandardError)}");
            }
        }



        private static string GetLoginToAwsDockerParams(GetAuthorizationTokenResponse authorizationToken)
        {
            // Check if we have any authorization token returned from AWS. We should have at least one.
            if (!authorizationToken.AuthorizationData.Any())
            {
                throw new Exception(
                    "Did not get authorization token from Amazon. " +
                    "Please try again later. If problem persist contact #watch-ops");
            }

            // Get password from authorization token
            var token = authorizationToken.AuthorizationData[0].AuthorizationToken;
            byte[] data = Convert.FromBase64String(token);
            string[] decodedToken = Encoding.UTF8.GetString(data)
                                            .Split(':');

            if (decodedToken.Length != 2)
            {
                throw new Exception(
                    "Retrieved wrong AWS authorization token. Cannot log into AWS. Please contact #watch-ops");
            }

            // Create docker loggin with AWS params
            var password = decodedToken[1];
            var proxyEndpoint = authorizationToken.AuthorizationData[0].ProxyEndpoint;
            return $"login -u AWS -p {password} {proxyEndpoint}";
        }
    }
}