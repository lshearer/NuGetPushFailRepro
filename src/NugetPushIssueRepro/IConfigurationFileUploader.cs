using System.Threading.Tasks;

namespace NugetPushIssueRepro
{
    internal interface IConfigurationFileUploader
    {
        string GenerateDestinationPath(string fileContents, string s3FileName);
        Task UploadConfiguration(string contents, string destinationPath);
        void UploadConfigurationFile(string filePath, string destinationPath);
    }
}