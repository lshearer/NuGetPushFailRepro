namespace NugetPushIssueRepro
{
    internal static class ContainerPaths
    {
        public const string MountedSourceDirectory = "/app-mount";
        public const string BuildDirectory = "/app-build";
        public const string BuildOutputDirectory = "/app-build-output";
        public static string DockerTaskScriptPath = "/app-util/dockerTask.sh";
        public static string NuGetPackagesCacheDirectory = "/root/.nuget/packages";
        public static string YarnCacheDirectory = "/usr/local/share/.cache/yarn";

        public static string GetWebappDirectory(MarvelMicroserviceConfig config)
        {
            return $"{BuildDirectory}/{config.RelativeWebappDirectory}";
        }
        public static string GetBrowserAppDirectory(MarvelMicroserviceConfig config)
        {
            return $"{BuildDirectory}/{config.RelativeWebappDirectory}/App";
        }
    }
}