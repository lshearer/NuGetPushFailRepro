using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NugetPushIssueRepro
{
    internal static class EcrResources
    {
        internal static class Internal
        {
            public static Lazy<string> BuildImageUri = new Lazy<string>(() =>
            {
                return ReadManifestResourceStream("NugetPushIssueRepro.Resources.InjectedArtifacts.dotnet_microservice_build.PublishedImageUrl.txt");
            });

            public static Lazy<string> HsdImageUri = new Lazy<string>(() =>
            {
                return ReadManifestResourceStream("NugetPushIssueRepro.Resources.InjectedArtifacts.hsd.PublishedImageUrl.txt");
            });
            
            private static string ReadManifestResourceStream(string resourceStreamName)
            {
                var assembly = typeof(Internal).GetTypeInfo().Assembly;
                var resourceStream = assembly.GetManifestResourceStream(resourceStreamName);

                using (var reader = new StreamReader(resourceStream, Encoding.UTF8))
                {
                    return reader.ReadToEnd().Trim();
                }
            }
        }

        public static AuthenticatedImageUri DotnetMicroserviceBuildImageUrl = new AuthenticatedImageUri(Internal.BuildImageUri);
        public static AuthenticatedImageUri HsdImageUrl = new AuthenticatedImageUri(Internal.HsdImageUri);

        public static AuthenticatedImageUri RegistratorLatestImageUrl = new AuthenticatedImageUri("761584570493.dkr.ecr.us-east-1.amazonaws.com/registrator:latest");

        public class AuthenticatedImageUri
        {
            private readonly Lazy<Task<AuthenticationResult>> _authenticatedUri;

            public AuthenticatedImageUri(string imageUri) : this(new Lazy<string>(() => imageUri))
            {
            }

            public AuthenticatedImageUri(Lazy<string> imageUri)
            {
                _authenticatedUri = new Lazy<Task<AuthenticationResult>>(async () =>
                {
                    // TODO - Only run authentication if the image is not already available locally? That would eliminate excessive auth calls,
                    // but could make it harder to spot when auth gets broken.
                    var isAuthenticated = await Security.EnsureAuthenticatedWithEcr();
                    if (isAuthenticated && IsRemoteUri(imageUri.Value))
                    {
                        Output.Info($"Pulling image {imageUri.Value}...");
                        await DockerCommands.PullImage(imageUri.Value);
                    }
                    return isAuthenticated ?
                        new AuthenticationResult(true, imageUri.Value) :
                        new AuthenticationResult(false, imageUri.Value);
                });
            }

            private bool IsRemoteUri(string imageUri)
            {
                // Assume only ECR URIs are remote
                return imageUri.Contains("amazonaws.com/");
            }

            public Task<AuthenticationResult> EnsureImageIsPulled()
            {
                return _authenticatedUri.Value;
            }

            public class AuthenticationResult
            {
                private readonly string _value;
                public AuthenticationResult(bool wasSuccess, string uri)
                {
                    WasSuccessful = wasSuccess;
                    _value = uri;
                }

                public bool WasSuccessful { get; private set; }
                public string Value
                {
                    get
                    {
                        if (!WasSuccessful)
                        {
                            throw new InvalidOperationException($"Attempted to access URL that was not authenticated. Url={_value}");
                        };
                        return _value;
                    }
                }
            }
        }
    }
}