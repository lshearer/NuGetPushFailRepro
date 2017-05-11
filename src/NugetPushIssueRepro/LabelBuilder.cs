using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NugetPushIssueRepro
{
    internal class LabelBuilder
    {
        private readonly MarvelMicroserviceConfig _serviceConfig;

        public LabelBuilder(MarvelMicroserviceConfig serviceConfig)
        {
            _serviceConfig = serviceConfig;
        }

        public async Task<Dictionary<string, string>> GetLabelsForLocalDev(string machineName)
        {
            return await CreateLabels(machineName, "123456");
        }

        public async Task<Dictionary<string, string>> GetLabels(BuildConfig buildConfig)
        {

            return await CreateLabels(buildConfig.BranchName, buildConfig.BuildNumber);
        }

        private async Task<Dictionary<string, string>> CreateLabels(string branchName, string buildNumber)
        {
            var serviceName = $"{_serviceConfig.ServiceName.ToUpper()}";

            if (string.IsNullOrWhiteSpace(branchName))
            {
                throw new ArgumentNullException(nameof(branchName));
            }
            if (string.IsNullOrWhiteSpace(buildNumber))
            {
                throw new ArgumentNullException(nameof(buildNumber));
            }

            return new Dictionary<string, string>{
                {"SERVICE_NAME", serviceName},
                {"SERVICE_REGISTER", "true"},
                {"SERVICE_EUREKA_METADATA.routes", GetRoutes()},
                {"SERVICE_EUREKA_METADATA_branch", SanitizeSubdomain(branchName)},
                {"SERVICE_EUREKA_METADATA_deploynumber", buildNumber},
                {"SERVICE_EUREKA_METADATA.version", await GetClientVersion(branchName, buildNumber)},
            };
        }


        private async Task<string> GetClientVersion(string branchName, string buildNumber)
        {
            return CombineVersionStrings(GetClientVersionPrefixString(), await GetClientVersionSuffix(branchName, buildNumber));
        }

        internal string GetClientVersionPrefixString()
        {
            var clientCsProjectPath = _serviceConfig.ClientCsProjAbsolutePath;

            XElement version;
            try
            {
                XDocument doc = XDocument.Parse(File.ReadAllText(clientCsProjectPath));
                version = doc.Descendants("VersionPrefix").First();
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to parse .csproj for client package at {clientCsProjectPath}.", e);
            }

            var versionString = version.Value;
            return versionString;
        }

        internal async Task<string> GetClientVersionSuffix(string branchName, string buildNumber)
        {
            await CommandUtilities.RunCommandAsync(
                "dotnet",
                "restore",
                workingDirectory: _serviceConfig.ClientPackageDirectory
            );

            var result = await CommandUtilities.RunCommandAsync(
                "dotnet",
                $"library create-version-suffix " +
                $"-b {branchName} -n {buildNumber}",
                workingDirectory: _serviceConfig.ClientPackageDirectory,
                errorMessage: "Failed to retrieve version suffix for client package"
            );

            // This is potentially fragile, as the first line of the output currently is "baseDir is ..."
            // Ideally we would only return the version suffix to stdout. We don't log anything
            // afterward in the library CLI main, so this is safe enough for now, but we might want to
            // change this in the future. If we can call the library code directly, we won't have this
            // issue anyway.
            return (result.StandardOutput.LastOrDefault() ?? "").Trim();
        }

        ///<summary>
        /// Combines a possible version suffix with the version defined in the *.csproj. This is
        /// following the behavior that dotnet pack will use.
        ///</summary>
        internal static string CombineVersionStrings(string versionPrefix, string versionSuffix)
        {
            return $"{versionPrefix }-{versionSuffix}".TrimEnd('-');
        }

        private string GetRoutes()
        {
            var serviceYaml = new ServiceConfigurationProvider(_serviceConfig.ServiceConfigFilePath).Load();
            var routes = serviceYaml.Infrastructure.Web.Routes;
            return string.Join("|", routes);
        }

        /// <summary>
        /// Sanitize a string to be usable as a subdomain label (dot-separated segment).
        /// </summary>
        internal static string SanitizeSubdomain(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new Exception($"{nameof(input)} cannot be null or empty.");
            }

            var sanitized = new Regex("[^a-zA-Z0-9-]").Replace(input, "")
                .Trim('-')
                .ToLower();

            const int maxLength = 63;
            if (sanitized.Length > maxLength)
            {
                sanitized = sanitized.Substring(0, maxLength);
            }

            if (!sanitized.Any())
            {
                throw new Exception($"{nameof(input)} ({input}) must contain at least one character in the sets a-z, A-Z, 0-9, or `-`. ");
            }
            if (new Regex(@"^\d+$").IsMatch(sanitized))
            {
                throw new Exception($"Sanitized subdomain must not be all numbers. Input={input} Sanitized={sanitized}");
            }

            return sanitized;
        }
    }
}