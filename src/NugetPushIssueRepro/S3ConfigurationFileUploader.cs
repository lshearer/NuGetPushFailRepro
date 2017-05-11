using System.IO;
using YamlDotNet.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using System.Threading;
using System.Threading.Tasks;
using NugetPushIssueRepro.ServiceConfigurationModel;

namespace NugetPushIssueRepro
{
    internal class S3ConfigurationFileUploader : IConfigurationFileUploader
    {
        public S3ConfigurationFileUploader(string uploadBucket) {
            UploadBucket = uploadBucket;
        }

        private IAmazonS3 _s3Client;
        public string UploadBucket {get;set;}
        private int s3UploadTimeoutMs = 3000;
        public string GenerateDestinationPath(string fileContents, string s3FileName)
        {
            // Read File Contents
            var deserializer = new DeserializerBuilder()
                                    .IgnoreUnmatchedProperties()
                                    .Build();
            var serviceConfig = deserializer.Deserialize<ServiceConfigFile>(fileContents);
           
            // get values needed.
            var serviceName = serviceConfig.Service.Name;
            var environment = serviceConfig.Environment;
            var branch = serviceConfig.Build.Branch;
            var commit = serviceConfig.Build.HeadCommit;
            var buildNumber = serviceConfig.Build.BuildNumber;
           
            // generate path name
            var path= $"ServiceDefinitions/{environment}/{serviceName}/{branch}/{s3FileName}.yaml";
            return path;
        }

        public async void UploadConfigurationFile(string filePath, string destinationPath) 
        {
            using(TextReader textReader = File.OpenText(filePath)) {
                string fileContents = textReader.ReadToEnd();
                await UploadConfiguration(fileContents, destinationPath);
            }
        }
        public async Task UploadConfiguration(string contents, string destinationPath)
        {
            if (_s3Client == null) {
                _s3Client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1);
            }

            var cts = new CancellationTokenSource();
            cts.CancelAfter(s3UploadTimeoutMs);

            PutObjectRequest request = new PutObjectRequest()
            {
                BucketName = UploadBucket,
                Key = destinationPath,
                ContentBody = contents
            };
            try {
                Output.Verbose($"Writing object to {request.Key}");
                PutObjectResponse result =  await _s3Client.PutObjectAsync(request, cts.Token);
                if (result.HttpStatusCode != System.Net.HttpStatusCode.OK ) {
                    throw new System.Exception($"Unable to upload config file to s3 {result.ToString()}");
                }
                Output.Success($"Service Configuraiton written to {request.Key}");
            } catch (System.Exception ex) 
            {
                throw new System.Exception("Unable to upload config file to s3", ex);
            }
        }
    }
}