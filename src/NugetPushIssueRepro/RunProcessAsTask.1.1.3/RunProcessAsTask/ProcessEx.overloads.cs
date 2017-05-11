using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RunProcessAsTask
{
    // these overloads match the ones in Process.Start to make it a simpler transition for callers
    // see http://msdn.microsoft.com/en-us/library/system.diagnostics.process.start.aspx
    internal partial class ProcessEx
    {
        public static Task<ProcessResults> RunAsync(string fileName)
        {
            return RunAsync(new ProcessStartInfo(fileName));
        }

        public static Task<ProcessResults> RunAsync(string fileName, string arguments, string workingDirectory = null, IDictionary<string, string> environmentOverrides = null)
        {
            var startInfo = new ProcessStartInfo(fileName, arguments);
            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                startInfo.WorkingDirectory = workingDirectory;
            }
            if (environmentOverrides != null) {
                foreach (var kvp in environmentOverrides) {
                    startInfo.Environment[kvp.Key] = kvp.Value;
                }
            }
            return RunAsync(startInfo);
        }

        // public static Task<ProcessResults> RunAsync(string fileName, string userName, SecureString password, string domain)
        // {
        //     return RunAsync(new ProcessStartInfo(fileName)
        //     {
        //         UserName = userName,
        //         Password = password,
        //         Domain = domain,
        //         UseShellExecute = false
        //     });
        // }

        // public static Task<ProcessResults> RunAsync(string fileName, string arguments, string userName, SecureString password, string domain)
        // {
        //     return RunAsync(new ProcessStartInfo(fileName, arguments)
        //     {
        //         UserName = userName,
        //         Password = password,
        //         Domain = domain,
        //         UseShellExecute = false
        //     });
        // }
    }
}
