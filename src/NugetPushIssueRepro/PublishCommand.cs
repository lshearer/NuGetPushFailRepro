using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NugetPushIssueRepro.StaticAssets;
using NugetPushIssueRepro.Utility;
using Microsoft.Extensions.CommandLineUtils;

namespace NugetPushIssueRepro
{
    internal class PublishCommand : CliCommand
    {
        internal override string CommandName => "publish";

        internal override string Description => "Build and publish production-ready microservice docker image for distribution.";

        private CommandOption _branchOption;
        private CommandOption _buildNumberOption;
        private CommandOption _nugetApiKeyOption;
        private CommandOption _awsAccessKeyOption;
        private CommandOption _awsAccessSecretOption;
        private CommandOption _gitShaOption;
        private CommandOption _mergeServiceConfig;
        private CommandOption _mergeAndUploadServiceConfig;
        private CommandOption _persistStaticAssetsOption;
        private readonly IBuildConfigurationBuilder _buildConfigurationBuilder;
        private readonly IConfigurationFileUploader _configurationFileUploader;
        private readonly IConfigurationFileMerger _configurationFileMerger;
        private readonly IConfigurationFileValidator _configurationFileValidator;
        private readonly IStaticAssetProcessor _staticAssetProcessor;
        private readonly IAccessor<S3AssetHostConfiguration> _assetHostConfigurationAccessor;

        public PublishCommand(
            IBuildConfigurationBuilder buildConfigurationBuilder,
            IConfigurationFileMerger configurationFileMerger,
            IConfigurationFileValidator configurationFileValidator,
            IConfigurationFileUploader configurationFileUploader,
            IStaticAssetProcessor staticAssetProcessor,
            IAccessor<S3AssetHostConfiguration> assetHostConfigurationAccessor)
        {
            _buildConfigurationBuilder = buildConfigurationBuilder.ThrowIfNull(nameof(buildConfigurationBuilder));
            _configurationFileUploader = configurationFileUploader.ThrowIfNull(nameof(configurationFileUploader));
            _configurationFileMerger = configurationFileMerger.ThrowIfNull(nameof(configurationFileMerger));
            _configurationFileValidator = configurationFileValidator.ThrowIfNull(nameof(configurationFileValidator));
            _staticAssetProcessor = staticAssetProcessor.ThrowIfNull(nameof(staticAssetProcessor));
            _assetHostConfigurationAccessor = assetHostConfigurationAccessor.ThrowIfNull(nameof(assetHostConfigurationAccessor));
        }

        protected override void CreateOptions(CommandLineApplication command)
        {
            _branchOption = command.Option("-b | --branch <BRANCH>",
                "Git branch for the current build." +
                "\nFor master branch or empty branch package will be published without suffix.",
                CommandOptionType.SingleValue);

            _buildNumberOption = command.Option("-n | --build <BUILD_NUMBER>",
                "CI unique build number.",
                CommandOptionType.SingleValue);

            _nugetApiKeyOption = command.Option("-k | --nuget-key <NUGET_API_KEY>",
                "Nuget api key for publishing.",
                CommandOptionType.SingleValue);

            _awsAccessKeyOption = command.Option("-a | --aws-key <AWS_ACCESS_KEY_ID>",
                "AWS access key id for access to AWS.",
                CommandOptionType.SingleValue);

            _awsAccessSecretOption = command.Option("-s | --aws-secret <AWS_SECRET>",
                "AWS secret access key for access to AWS.",
                CommandOptionType.SingleValue);

            _gitShaOption = command.Option("-g | --git-sha <GIT_SHA>",
                "SHA hash for checked out git commit.",
                CommandOptionType.SingleValue);

            _mergeServiceConfig = command.Option("--service-config-merge-only",
                "Merges service configuration files, but does not upload them to s3",
                CommandOptionType.NoValue);

            _mergeAndUploadServiceConfig = command.Option("--service-config-upload",
                "Merges service configuration files, and uploads them to S3",
                CommandOptionType.NoValue);

            _persistStaticAssetsOption = command.Option("--persist-static-assets",
                "Publishes static assets to a persistent CDN. This option should be enabled for all deployable builds (e.g., CI builds for both prod and thor). " +
                "Default behavior publishes assets to non-persistent, short-term storage for local builds and automated testing of the build tools.",
                CommandOptionType.NoValue);
        }

        protected override async Task<int> Run(MarvelMicroserviceConfig config)
        {
            foreach (var kvp in new Dictionary<string, CommandOption>{
                {"branch", _branchOption},
                {"git SHA", _gitShaOption},
                {"build number", _buildNumberOption},
                {"NuGet API key", _nugetApiKeyOption},
                {"AWS access key id", _awsAccessKeyOption},
                {"AWS secret key", _awsAccessSecretOption},
                {"persist static assets", _persistStaticAssetsOption},
            })
            {
                Output.Verbose($"Option {kvp.Key}: {kvp.Value.Value()}");
            }

            var paramValidationErrors = ValidateParams();
            if (paramValidationErrors.Any())
            {
                paramValidationErrors.ForEach(Output.Error);
                return 1;
            }

            var persistStaticAssets = _persistStaticAssetsOption.HasValue();

            if (!persistStaticAssets)
            {
                Output.Info($"NOTE: Static assets will not be published to a persistent CDN. They will be temporarily available (minimum of 1 day) for testing with this build. " +
                $"If this build is for a deployable app (e.g., a CI build for either prod or thor environments, or a locally-built emergency deploy), " +
                $"you should specify the {_persistStaticAssetsOption.LongName} option. If this is a local build for manual testing or an automated testing run, you may ignore this.");
            }

            _assetHostConfigurationAccessor.SetValue(persistStaticAssets ? S3AssetHostConfiguration.ProductionAssets : S3AssetHostConfiguration.TestAssets);

            return await Publisher.Publish(config, _buildConfigurationBuilder, _configurationFileMerger, _configurationFileUploader, _configurationFileValidator, _staticAssetProcessor, _nugetApiKeyOption.Value(), _awsAccessKeyOption.Value(), _awsAccessSecretOption.Value(), _branchOption.Value(), _gitShaOption.Value(), _buildNumberOption.Value(), _mergeAndUploadServiceConfig.HasValue(), _mergeServiceConfig.HasValue());
        }

        private List<string> ValidateParams()
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(_branchOption.Value()))
            {
                validationErrors.Add("Missing branch argument. See --help for details.");
            }
            if (string.IsNullOrWhiteSpace(_gitShaOption.Value()))
            {
                validationErrors.Add("Missing git SHA argument. See --help for details.");
            }
            if (string.IsNullOrWhiteSpace(_buildNumberOption.Value()))
            {
                validationErrors.Add("Missing build number argument. See --help for details.");
            }
            if (string.IsNullOrWhiteSpace(_nugetApiKeyOption.Value()))
            {
                validationErrors.Add("Missing NuGet API key argument. See --help for details.");
            }
            if (string.IsNullOrWhiteSpace(_awsAccessKeyOption.Value()))
            {
                validationErrors.Add("Missing AWS access key id argument. See --help for details.");
            }
            if (string.IsNullOrWhiteSpace(_awsAccessSecretOption.Value()))
            {
                validationErrors.Add("Missing AWS secret access key argument. See --help for details.");
            }

            return validationErrors;
        }
    }
}
