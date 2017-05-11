using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace NugetPushIssueRepro
{
    internal class ImageNameBuilder
    {
        public static ImageNameBuilderResult CreateImageNameAndTag(string serviceName, string branchName, string gitCommitSha, DateTime time, string buildNumber)
        {
            var repository = "761584570493.dkr.ecr.us-east-1.amazonaws.com";
            var tag = CreateVersionTag(branchName, gitCommitSha, buildNumber, time);
            var imageName = ConvertServiceName(serviceName);
            var result = new ImageNameBuilderResult(repository, imageName, tag);

            // This shouldn't ever happen, so we'll just do a simple check on the result instead of validating each input.
            if (Regex.IsMatch(result.FullPath, @"\s"))
            {
                throw new Exception($"Version tag contains whitespace. One or more inputs are not valid. Tag={result.FullPath}");
            }
            return result;
        }

        internal static string ConvertServiceName(string serviceName)
        {
            if (serviceName.Contains("-")) throw new FormatException("A service name cannot have dashes.");
            if (serviceName.Contains(".")) throw new FormatException("A service name cannot have periods.");
            if (serviceName.Contains(" ")) throw new FormatException("A service name cannot have spaces.");
            if (string.IsNullOrWhiteSpace(serviceName)) throw new FormatException("A service name cannot be blank.");
            return serviceName.ToLower();
        }

        private static string SanitizeBranchName(string branchName)
        {
            var excludedBranchNameChars = new Regex("[^a-zA-Z0-9_-]");
            branchName = excludedBranchNameChars.Replace(branchName, "_");
            branchName = branchName.TrimStart('-');
            return branchName;
        }

        internal static string CreateVersionTag(string branchName, string gitCommitSha, string buildNumber, DateTime time)
        {
            var shortSha = gitCommitSha.Substring(0, 7).ToLower();
            var formattedDate = time.ToUniversalTime().ToString("yyyyMMdd-HHmm");

            var tag = $".{formattedDate}.{shortSha}.{buildNumber}";
            var tagMaxLength = 128;
            var sanitizedBranch = string.Join("", SanitizeBranchName(branchName).Take(tagMaxLength - tag.Length));
            tag = $"{sanitizedBranch}{tag}";

            return tag;
        }
    }
}
