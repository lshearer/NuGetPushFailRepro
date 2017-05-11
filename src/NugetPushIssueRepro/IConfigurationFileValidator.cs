using System.Threading.Tasks;

namespace NugetPushIssueRepro
{
    internal interface IConfigurationFileValidator
    {
        Task<IValidatorResults> ValidateConfigurationFile(string baseDirectory, string fileToValidate);

    }
}