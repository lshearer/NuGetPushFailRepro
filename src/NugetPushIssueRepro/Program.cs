using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Amazon.S3;
using NugetPushIssueRepro.StaticAssets;
using NugetPushIssueRepro.Utility;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace NugetPushIssueRepro
{
    public class Program
    {
        internal static string CliCommandName => "marvel";

        public static int Main(string[] args)
        {
            try
            {
                var app = new CommandLineApplication
                {
                    Name = $"dotnet-{CliCommandName}",
                };

                CliCommand.AddHelpOption(app);
                var verboseOption = CliCommand.AddVerboseOption(app);

                var currentDir = Directory.GetCurrentDirectory();
                GetCommands().ForEach(command => command.Register(app, currentDir, verboseOption));
                new IssueTestClass();
                app.OnExecute(() =>
                {
                    app.ShowHelp();
                    return 0;
                });

                // return app.Execute(args);
            }
            catch (Exception e)
            {
                Output.Exception(e);
                return 1;
            }
            Console.WriteLine("Hello");
            return 0;
        }

        internal static List<CliCommand> GetCommands()
        {
            var services = BuildDefaultServices().BuildServiceProvider(true);

            return new List<CliCommand>{
                    new WatchCommand(),
                    new LaunchCommand(),
                    new DebugCommand(),
                    new AuthorizeCommand(),
                    // services.GetRequiredService<BuildCommand>(),
                    // services.GetRequiredService<PublishCommand>(),
                };
        }

        internal static IServiceCollection BuildDefaultServices()
        {
            var services = new ServiceCollection();
            // services.AddSingleton<BuildCommand, BuildCommand>();
            // services.AddSingleton<PublishCommand, PublishCommand>();

            // // Using IFileProvider for the interface abstraction, but setting the root at system root because we're not looking
            // // for access restriction and we don't have a consistent "root" (webapp project root, webapp content root, system root,
            // // test fixture root, etc.) to create relative paths for our file path requests. We could create separate marker
            // // interfaces (e.g., IWebappRootFileProvider : IFileProvider) under which to store separate file provider instances
            // // with these different "roots" if it feels nicer as we add more usage.
            // services.AddSingleton<IFileProvider>(provider => new PhysicalFileProvider(Path.GetPathRoot(Directory.GetCurrentDirectory())));
            // services.AddSingleton<IHashUtility, Sha256HashUtility>();
            // services.AddSingleton<IStaticAssetManifestSerializer, DefaultStaticAssetManifestSerializer>();
            // services.AddSingleton<IConsole, DefaultConsole>();
            // services.AddSingleton<IFileSystem, PhysicalFileSystem>();
            // services.AddSingleton<IStaticAssetHost, S3StaticAssetHost>();
            // services.AddSingleton<HttpClient, HttpClient>();

            // // I don't anticipate needing other regions, but if we can add more interfaces that extend IAmazonS3 just as markers
            // services.AddSingleton<IAmazonS3>(provider => new AmazonS3Client(Amazon.RegionEndpoint.USEast1));
            // services.AddSingleton<IAccessor<S3AssetHostConfiguration>, BasicAccessor<S3AssetHostConfiguration>>();
            // services.AddSingleton<IStaticAssetProcessor, StaticAssetProcessor>();
            // services.AddSingleton<IStaticAssetUploader, S3StaticAssetUploader>();
            // services.AddSingleton<IConfigurationFileMerger, HSDConfigurationFileMerger>();
            // services.AddSingleton<IConfigurationFileValidator, HSDConfigurationFileValidator>();
            // services.AddSingleton<IBuildConfigurationBuilder, YamlBuildConfigurationBuilder>();

            return services;
        }
    }
}
