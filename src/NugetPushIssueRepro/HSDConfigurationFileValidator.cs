using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
namespace NugetPushIssueRepro
{
    internal class HSDConfigurationFileValidator : IConfigurationFileValidator
    {
        public async Task<IValidatorResults> ValidateConfigurationFile(string baseDirectory, string fileToValidate)
        {
            var buildImageUri = await EcrResources.HsdImageUrl.EnsureImageIsPulled();
            if (!buildImageUri.WasSuccessful)
            {
                throw new Exception($"Unable to get image {buildImageUri.Value} from ECR");
            }
            var validationOutputFile = $"{fileToValidate}.validation-output";
            var dockerRunOptions = new List<string>
            {
                $"--rm",
                $"-v {baseDirectory}:{ContainerPaths.MountedSourceDirectory}"
            };

            var hsdValidateCommand = $"/bin/bash hsd-validate.sh {ContainerPaths.MountedSourceDirectory}/{fileToValidate} {ContainerPaths.MountedSourceDirectory}/{validationOutputFile}";
            var exitCode = CommandUtilities.ExecuteCommand("docker", $"run {string.Join(" ", dockerRunOptions)} {buildImageUri.Value} {hsdValidateCommand}");
            if (exitCode != 0)
            {
                throw new Exception("HSD Validation failed.");
            }
            var outputFilePath = Path.Combine(baseDirectory, validationOutputFile);
            var outputContents = File.ReadAllText(outputFilePath);
            try 
            {
                File.Delete(outputFilePath);
            } 
            catch (Exception ex) 
            {
                Output.Error($"Unable to delete validation output file: {ex.Message}");
            }
            var validationResult = new ValidationResult(outputContents);

            return validationResult;
        }
    }
    internal class ValidationResult : IValidatorResults 
    {
        internal ValidationResult(string response) {
            Response = response;
            if(response.Contains("Looks like everything is good to go!")) {
                IsValid = true;
            }
        }
        public bool IsValid {get; private set;}
        public string Response {get; private set;}
    }
}