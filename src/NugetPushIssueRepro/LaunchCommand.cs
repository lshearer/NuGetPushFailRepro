using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using RunProcessAsTask;

namespace NugetPushIssueRepro
{
    internal class LaunchCommand : CliCommand
    {

        // TODO - change this to the Thor URL once it's safe to.
        private const string DefaultEurekaServerUrl = "http://localhost:8080/eureka/v2";
        private const string ThorVPCEurekaServerUrl = "";
        private const string ThorClassicEurekaServerUrl = "";
        private const string DefaultIpAddress = "localhost";
        private const string IpifyUrl = "";
        private static HttpClient _httpClient;

        public class LaunchOptions
        {
            public string Host { get; set; }
            public bool UseSharedCache { get; set; }
            public string EurekaServer { get; set; }
            public string LocalIpAddress { get; set; }
        }

        public LaunchCommand()
        {
            if (_httpClient != null) return;
            _httpClient = new HttpClient(new HttpClientHandler
            {
                UseProxy = false
            })
            {
                Timeout = TimeSpan.FromSeconds(1)
            };
        }

        public class ValidatedOptions<TOptions> where TOptions : class
        {
            public bool AreValid { get; private set; }

            public ValidatedOptions(bool areValid, TOptions options = null)
            {
                _options = options;
                AreValid = areValid;
                if (areValid && options == null)
                {
                    throw new InvalidOperationException($"{nameof(options)} must not be null if {nameof(areValid)} is true.");
                }
            }

            private TOptions _options;
            public TOptions Value
            {
                get
                {
                    if (!AreValid)
                    {
                        throw new InvalidOperationException($"Attempt to access {nameof(Value)} for an invalid result. Code should check {nameof(AreValid)} first.");
                    }
                    return _options;
                }
            }
        }

        public class LaunchOptionsGroup
        {

            private CommandOption _hostOption;
            private CommandOption _mountCacheDirectoryOption;
            private CommandOption _eurekaServerOption;
            private CommandOption _localIpAddressOption;
            public LaunchOptionsGroup(CommandLineApplication command)
            {
                // TODO - this should probably be a machine-wide config, not a per-service config.
                _hostOption = command.Option("--host <HOST>",
                    "Hostname to be used in URLs for routing to this app.",
                    CommandOptionType.SingleValue);

                _mountCacheDirectoryOption = command.Option("--use-shared-cache",
                    "Use shared cache directory between container instances.",
                    CommandOptionType.NoValue);

                _eurekaServerOption = command.Option("--eureka-server <EUREKA_SERVER_URL>",
                    "Url for the Eureka server to connect to, e.g. http://localhost:8080/eureka/v2.",
                    CommandOptionType.SingleValue);

                _localIpAddressOption = command.Option("--ip <IP>",
                    "The ip address that registrator should use when registering your service with eureka. This is most commonly your office IP address or your thor-vpn IP address.",
                    CommandOptionType.SingleValue);

            }

            internal static List<string> ValidateEurekaServerUrl(string url, string optionName)
            {
                var errors = new List<string>();
                if (url == null)
                {
                    return errors;
                }
                Uri uri;
                if (Uri.TryCreate(url, UriKind.Absolute, out uri)
                    && !string.IsNullOrWhiteSpace(uri.Scheme)
                    && !string.IsNullOrWhiteSpace(uri.Host))
                {
                    return errors;
                }
                return new List<string> { $"{optionName} parameter: value ({url}) must be an absolute URL." };
            }

            internal static List<string> ValidateIpAddress(string ip, string eurekaServer, string optionName)
            {
                if (string.IsNullOrWhiteSpace(ip)) return new List<string> { $"{optionName} parameter: value can't be empty." };
                if (ip == DefaultIpAddress)
                {
                    var eurekaServerToUse = eurekaServer ?? DefaultEurekaServerUrl;
                    if (eurekaServerToUse == ThorClassicEurekaServerUrl || eurekaServerToUse == ThorVPCEurekaServerUrl)
                    {
                        return new List<string>
                        {
                            $"When using the Thor Eureka server ({DefaultEurekaServerUrl}) it is recommended to use your Thor VPN IP Address, rather than {DefaultIpAddress}. We were unable to detect your Thor VPN IP, are you connected?"
                        };
                    }
                    return new List<string>();
                }
                IPAddress ipAddress;
                if (!IPAddress.TryParse(ip, out ipAddress))
                {
                    return new List<string> { $"{optionName} parameter: value ({ip}) must be a valid IP Address." };
                }
                if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    return new List<string> { $"{optionName} parameter: value ({ip}) must be a valid IPv4 Address, IPv6 isn't supported." };
                }
                return new List<string>();
            }

            private List<string> ValidateOptions(LaunchOptions options)
            {
                var validation = ValidateEurekaServerUrl(options.EurekaServer, _eurekaServerOption.LongName);
                validation.AddRange(ValidateIpAddress(options.LocalIpAddress, options.EurekaServer, _localIpAddressOption.LongName));
                return validation;
            }

            // Ipify is a service running on the thor vpc that will return the users thor vpn ip.
            private async Task<string> FetchIpifyAddress()
            {
                try
                {
                    var result = await _httpClient.GetAsync(IpifyUrl);
                    if (result.StatusCode != HttpStatusCode.OK)
                    {
                        Output.Info($"Failed to get VPN IP address. You're likely to need this.\nIpify returned a StatusCode of {result.StatusCode}");
                        return DefaultIpAddress;
                    }
                    return await result.Content.ReadAsStringAsync();
                }
                catch (Exception e)
                {
                    Output.Info($"Failed to get VPN IP address. You're likely to need this.\nExceptionMessage={e.Message}");
                    return DefaultIpAddress;
                }
            }
            private static void LogValidationErrors(List<string> errors)
            {
                errors.ForEach(error => Output.Error($"Argument validation error: {error}"));
            }

            public async Task<ValidatedOptions<LaunchOptions>> ProcessOptions()
            {
                var options = new LaunchOptions
                {
                    Host = _hostOption.HasValue() ? _hostOption.Value() : null,
                    UseSharedCache = _mountCacheDirectoryOption.HasValue(),
                    EurekaServer = _eurekaServerOption.HasValue() ? _eurekaServerOption.Value() : null,
                    LocalIpAddress = _localIpAddressOption.HasValue() ? _localIpAddressOption.Value() : null
                };

                if (options.LocalIpAddress == null)
                {
                    options.LocalIpAddress = await FetchIpifyAddress();
                }
                var errors = ValidateOptions(options);
                if (errors.Any())
                {
                    LogValidationErrors(errors);
                    return new ValidatedOptions<LaunchOptions>(false);
                }
                return new ValidatedOptions<LaunchOptions>(true, options);
            }
        }
        private LaunchOptionsGroup _launchOptions;
        internal override string CommandName => "launch";

        internal override string Description => "Launch container for watching and running local project (VS Code usage)";

        protected override void CreateOptions(CommandLineApplication command)
        {
            _launchOptions = new LaunchOptionsGroup(command);
        }

        protected override async Task<int> Run(MarvelMicroserviceConfig config)
        {
            var options = await _launchOptions.ProcessOptions();
            if (!options.AreValid)
            {
                return 1;
            }
            return await Launch(config, options.Value);
        }

        public static async Task<int> Launch(MarvelMicroserviceConfig config, LaunchOptions launchOptions)
        {
            var registratorRunResultTask = EnsureRegistratorContainerIsRunning(launchOptions.EurekaServer, launchOptions.LocalIpAddress);
            var buildImageUriTask = EcrResources.DotnetMicroserviceBuildImageUrl.EnsureImageIsPulled();

            var registratorRunResult = await registratorRunResultTask;
            if (registratorRunResult != 0)
            {
                return registratorRunResult;
            }

            var buildImageUri = await buildImageUriTask;
            if (!buildImageUri.WasSuccessful)
            {
                return 1;
            }

            // # TODO include a hash of the tools version in the container name to ensure containers are recreated after tools update(?)
            var containerName = config.DevContainerName;
            var dockerTaskScriptPath = ContainerPaths.DockerTaskScriptPath;

            // Build arguments list for `docker create` call so we can hash them and ensure an existing container is compatible. Argument pairs are combined into one argument for readability.
            var hostname = launchOptions.Host ?? await NetworkUtility.GetFriendlyHostName();
            var labelArgs = LabelUtilities.FormatLabelsAsArguments(await new LabelBuilder(config).GetLabelsForLocalDev(hostname));
            var dockerCreateArgs = labelArgs.Concat(new List<string>
            {
                "-p 5000:5000",
                "--dns-search=agilesports.local",
                $"-e \"{ProjectNameEnvVariable}={config.ProjectName}\"",
                $"-e \"{SolutionNameEnvVariable}={config.SolutionName}\"",
                $"-e \"{WebappDirEnvVariable}={ContainerPaths.GetWebappDirectory(config)}\"",
                $"-e \"{BrowserAppDirEnvVariable}={ContainerPaths.GetBrowserAppDirectory(config)}\"",
                // This could probably live in the image
                $"-e ASPNETCORE_ENVIRONMENT=Development",
                $"-v {config.BaseDirectory}:{ContainerPaths.MountedSourceDirectory}",
            });
            if (launchOptions.UseSharedCache)
            {
                dockerCreateArgs = dockerCreateArgs.Concat(GetCacheVolumeArgs());
            }

            var createContainerResult = await DockerCommands.GetOrCreateContainerWithName(
                containerName,
                buildImageUri.Value,
                dockerCreateArgs.ToList(),
                $"/bin/bash {dockerTaskScriptPath} hang");

            var container = createContainerResult.Container;
            var isNewContainer = createContainerResult.IsNewContainer;

            if (!container.IsRunning)
            {
                Output.Info($"Starting container {containerName}");
                var result = await CommandUtilities.RunCommandAsync("docker", $"start {container.ContainerId}", throwOnErrorExitCode: false);
                // Output.Info("StdErr=" + string.Join("\n", result.StandardError));
                // Output.Info("StdOut=" + string.Join("\n", result.StandardOutput));
                if (result.ExitCode != 0)
                {
                    var stdErr = string.Join("\n", result.StandardError);
                    // Message is `Bind for 0.0.0.0:5000 failed: port is already allocated`. trimming the port portion in case the port changes.
                    if (stdErr.Contains("failed: port is already allocated"))
                    {
                        Output.Info("Webapp port is already in use. Attempting to stop other container using port.");
                        // Find other containers using a partial match on suffix. This corresponds to the naming scheme defined in MarvelMicroserviceConfig.
                        var containers = await DockerCommands.GetContainersByName("marvel-dev");
                        var otherContainer = containers.FirstOrDefault(c => c.IsRunning);
                        if (otherContainer == null)
                        {
                            Output.Error("Unable to find running container using same port.");
                            Output.Error($"Failed to start container {containerName}. StandardError={stdErr}");
                            return 1;
                        }
                        Output.Info($"Stopping container {otherContainer.ContainerId}");
                        await DockerCommands.StopContainer(otherContainer);

                        Output.Info($"Starting container {containerName} again");
                        var restartResult = await ProcessEx.RunAsync("docker", $"start {container.ContainerId}");
                        if (restartResult.ExitCode != 0)
                        {
                            Output.Error($"Failed to restart container {containerName}. StandardError={stdErr}");
                            return result.ExitCode;
                        }
                    }
                    else
                    {
                        Output.Error($"Failed to start container {containerName}. StandardError={stdErr}");
                        return result.ExitCode;
                    }
                }

                // TODO only perform this check after failed `docker exec` commands, for better reporting?
                // Ensure the container doesn't immediately exit.
                Thread.Sleep(10);

                var runningContainer = await DockerCommands.GetContainerByName(containerName);
                if (!runningContainer.IsRunning)
                {
                    Output.Error($"Container {containerName} stopped unexpectedly. Check container logs.");
                    return 1;
                }
            }

            if (isNewContainer)
            {
                Output.Info($"Attaching to container {containerName} to run first time launch command on new container");
                var code = CommandUtilities.ExecuteCommand("docker", $"exec -i {container.ContainerId} /bin/bash {dockerTaskScriptPath} firstTimeLaunch");
                if (code != 0)
                {
                    Output.Info($"First time startup command failed. Removing container.");
                    await DockerCommands.RemoveContainer(container);
                    return code;
                }
            }
            else
            {
                Output.Info($"Attaching to container {containerName} to run relaunch command on existing container");
                var code = CommandUtilities.ExecuteCommand("docker", $"exec -i {container.ContainerId} /bin/bash {dockerTaskScriptPath} relaunch");
                if (code != 0)
                {
                    return code;
                }
            }

            Output.Info($"Container {containerName} launched and ready to run");
            Output.Info($"Using hostname: {hostname}");
            Output.Info($"If debugging from VS Code, switch to the Debug Console (Cmd+Shift+Y / Ctrl+Shift+Y) for app and watch process logs");

            return 0;
        }

        private static List<string> GetCacheVolumeArgs()
        {
            var cacheDir = GetMarvelCacheDirectory();
            Output.Info($"Using cache directory: {cacheDir}");
            return new List<string>
            {
                $"-v {Path.Combine(cacheDir, "nuget", "packages")}:{ContainerPaths.NuGetPackagesCacheDirectory}",
                $"-v {Path.Combine(cacheDir, "yarn")}:{ContainerPaths.YarnCacheDirectory}"
            };
        }

        private static string GetMarvelCacheDirectory()
        {
            // From https://github.com/aspnet/Configuration/blob/1dd72cdaa44358ba917608f78bc7715fe33526ae/src/Microsoft.Extensions.Configuration.UserSecrets/PathHelper.cs#L103-L104
            var appDataDir = Environment.GetEnvironmentVariable("APPDATA");
            var homeDir = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrWhiteSpace(appDataDir))
            {
                return Path.Combine(appDataDir, "", "", "Cache");
            }
            else
            {
                return Path.Combine(homeDir, ".", "", "cache");
            }
        }

        private static async Task<int> EnsureRegistratorContainerIsRunning(string eurekaServerUrl, string ipAddress)
        {
            var registratorImageUri = await EcrResources.RegistratorLatestImageUrl.EnsureImageIsPulled();
            if (!registratorImageUri.WasSuccessful)
            {
                return 1;
            }

            var registratorContainerName = "registrator-marvel";

            // Build arguments list for `docker create` call so we can hash them and ensure an existing container is compatible. Argument pairs are combined into one argument for readability.
            var dockerCreateArgs = new List<string>
            {
                "--net=host",
                "--volume=/var/run/docker.sock:/tmp/docker.sock"
            };


            var createContainerResult = await DockerCommands.GetOrCreateContainerWithName(
                registratorContainerName,
                registratorImageUri.Value,
                dockerCreateArgs,
                $"-ttl 30 -ttl-refresh 15 -ip {ipAddress} -require-label {FixEurekaServerUrlScheme(eurekaServerUrl ?? DefaultEurekaServerUrl)}");

            var container = createContainerResult.Container;
            var isNewContainer = createContainerResult.IsNewContainer;

            if (!container.IsRunning)
            {
                Output.Info($"Starting container {registratorContainerName}");
                var result = await CommandUtilities.RunCommandAsync("docker", $"start {container.ContainerId}", throwOnErrorExitCode: false);
                if (result.ExitCode != 0)
                {
                    var stdErr = string.Join("\n", result.StandardError);
                    // Message is `Bind for 0.0.0.0:5000 failed: port is already allocated`. trimming the port portion in case the port changes.
                    // if (stdErr.Contains("failed: port is already allocated"))
                    // {
                    //     Output.Info("Webapp port is already in use. Attempting to stop other container using port.");
                    //     // Find other containers using a partial match on suffix. This corresponds to the naming scheme defined in MarvelMicroserviceConfig.
                    //     var containers = await DockerCommands.GetContainersByName("marvel-dev");
                    //     var otherContainer = containers.FirstOrDefault(c => c.IsRunning);
                    //     if (otherContainer == null)
                    //     {
                    //         Output.Error("Unable to find running container using same port.");
                    //         Output.Error($"Failed to start container {containerName}. StandardError={stdErr}");
                    //         return 1;
                    //     }
                    //     Output.Info($"Stopping container {otherContainer.ContainerId}");
                    //     await DockerCommands.StopContainer(otherContainer);

                    //     Output.Info($"Starting container {containerName} again");
                    //     var restartResult = await ProcessEx.RunAsync("docker", $"start {container.ContainerId}");
                    //     if (restartResult.ExitCode != 0)
                    //     {
                    //         Output.Error($"Failed to restart container {containerName}. StandardError={stdErr}");
                    //         return result.ExitCode;
                    //     }
                    // }
                    // else
                    // {
                    Output.Error($"Failed to start container {registratorContainerName}. StandardError={stdErr}");
                    return result.ExitCode;
                    // }
                }

                // Ensure the container doesn't immediately exit.
                // TODO Bumped this up for registrator specifically to ensure eureka host is valid. Might want to verify by scanning logs
                // that this did in fact start up properly.
                Thread.Sleep(500);

                var runningContainer = await DockerCommands.GetContainerByName(registratorContainerName);
                if (!runningContainer.IsRunning)
                {
                    Output.Error($"Container {registratorContainerName} stopped unexpectedly. Check container logs by running {DockerCommands.GetLogsForContainerCommand(runningContainer)}.");
                    return 1;
                }
            }

            // TODO - ensure registrator is ready? pause?

            Output.Info($"Container {registratorContainerName} is running.");

            return 0;
        }

        internal static string FixEurekaServerUrlScheme(string url)
        {
            // Change URL to start with eureka://
            var eurekaUri = new UriBuilder(url);
            eurekaUri.Scheme = "eureka";
            return eurekaUri.Uri.AbsoluteUri;
        }
    }
}
