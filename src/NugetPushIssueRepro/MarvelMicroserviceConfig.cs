using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NugetPushIssueRepro
{
    internal class MarvelMicroserviceConfig
    {
        public string ServiceConfigFileName {get; private set;} = "service.yaml";
        public string ServiceConfigFilePath {get; private set;}
        public string BuildConfigFileName {get; private set;} = "build.yaml";
        public string BuildConfigFilePath {get; private set;}
        // project name is read from the directory name.
        public string ProjectName { get; private set; }
        public string SolutionName { get; private set; }
        // service name is read from the service configuration file.
        public string ServiceName { get; private set; }
        public string WebappDirectory { get; private set; }
        public string RelativeWebappDirectory { get; private set; }
        public string ClientPackageDirectory { get; private set; }
        public string BaseDirectory { get; private set; }
        public string SourceDirectory { get; private set; }
        public string DevContainerName { get; private set; }
        public string BuildContainerName { get; private set; }
        public string DevPublishedRuntimeImageName { get; private set; }
        public string PublishedWebappAssemblyPath { get; private set; }
        public string DotnetPublishOutputPath { get; private set; }
        public string CsProjAbsolutePath { get; private set; }
        public string ClientCsProjAbsolutePath { get; private set; }

        public MarvelMicroserviceConfig(string currentDirectory)
        {
            var baseDirectory = GetSolutionRootDirectoryPath(currentDirectory);
            var projectNameInfo = GetProjectName(baseDirectory);

            BaseDirectory = baseDirectory;
            ProjectName = projectNameInfo.ProjectName;
            SolutionName = $"{projectNameInfo.FullProjectName}.Webapp.sln";
            RelativeWebappDirectory = $"src/{projectNameInfo.FullProjectName}.Webapp";
            WebappDirectory = Path.Combine(baseDirectory, RelativeWebappDirectory);
            CsProjAbsolutePath = Path.Combine(WebappDirectory, $"{projectNameInfo.FullProjectName}.Webapp.csproj");
            SourceDirectory = Path.Combine(baseDirectory,"src/");
            ClientPackageDirectory = Path.Combine(SourceDirectory, $"{projectNameInfo.FullProjectName}.Client");
            ClientCsProjAbsolutePath = Path.Combine(ClientPackageDirectory, $"{projectNameInfo.FullProjectName}.Client.csproj");
            DevContainerName = $"{projectNameInfo.ProjectName.ToLower()}-marvel-dev";
            DevPublishedRuntimeImageName = $"{projectNameInfo.ProjectName.ToLower()}:latest";
            BuildContainerName = $"{projectNameInfo.ProjectName.ToLower()}-marvel-build";
            DotnetPublishOutputPath = $"{WebappDirectory}/bin/temp-docker-build";
            PublishedWebappAssemblyPath = $"{projectNameInfo.FullProjectName}.Webapp.dll";
            BuildConfigFilePath = Path.Combine(SourceDirectory, BuildConfigFileName);
            ServiceConfigFilePath = Path.Combine(SourceDirectory, ServiceConfigFileName);
            // read service Name from config file, if it doesn't exist use projectName
            ServiceName = GetServiceName(ServiceConfigFilePath) ?? ProjectName;
        }

        internal class ProjectNameInfo
        {
            /// <summary>
            /// The unique portion of the C# project names—e.g., Leroy, SpeedTest, etc.—used to derive all other names from.
            /// </summary>
            public string ProjectName { get; set; }
            /// <summary>
            /// The full project name prefix for all of the C# projects, optionally including a `Marvel` segment.
            /// </summary>
            /// <remarks>
            /// </remarks>
            public string FullProjectName { get; set; }
        }

        internal static string GetServiceName(string serviceConfigurationPath) {
            try {
                var serviceYaml = new ServiceConfigurationProvider(serviceConfigurationPath).Load();
                return serviceYaml.Service.Name;
            }
            catch (Exception ex)
            {
                Output.Error($"Unable to read serviceName {ex.Message}");
                return null;
            }
        }

        internal static ProjectNameInfo GetProjectName(string baseDirectory)
        {
            var sourceDirectory = Path.Combine(baseDirectory, "src");
            if (!Directory.Exists(sourceDirectory))
            {
                throw new Exception($"Could not find expected source directory: {sourceDirectory}.");
            }
            var webappDirPattern = "";
            var webappDirectory = Directory.GetDirectories(sourceDirectory, webappDirPattern).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(webappDirectory))
            {
                throw new Exception($"Could not locate the webapp project. Searched in {sourceDirectory} for a project matching {webappDirPattern}.");
            }

            var dir = Path.GetFileName(webappDirectory);
            var match = Regex.Match(dir, @"");
            var projectName = match.Groups[2]?.Value;
            var marvelSegment = match.Groups[1]?.Value;
            return new ProjectNameInfo
            {
                ProjectName = projectName,
                FullProjectName = $"",
            };
        }

        internal static string GetSolutionRootDirectoryPath(string startingDirectory)
        {
            // First get our current directory
            var baseDir = startingDirectory;
            do
            {
                // Check if we have a global.json file (are we in the root of the project?)
                // Note that alternative check could be for .git folder, but it will not be
                // generic enough (e.g. dothet library Cli couldn't be build with it then)
                if (Directory.GetFiles(baseDir, "global.json").Any())
                {
                    // We are in the root of the project
                    return baseDir;
                }

                // Let's try to go one level up
                var parentDir = Directory.GetParent(baseDir);
                if (parentDir == null)
                {
                    throw new Exception(
                        "Did not find global.json file." +
                        "\nAre you sure you are running me from the project folder?");
                }
                baseDir = parentDir.FullName;
            } while (true);
        }
    }
}