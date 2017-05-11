using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace NugetPushIssueRepro
{
    internal class NetworkUtility
    {
        public static async Task<string> GetFriendlyHostName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Using LocalHostName config on a Mac because HostName commonly isn't set, resulting in something like "ip-192-168-1-127"
                // from Environment.MachineName (as well as the `hostname` command). LocalHostName is also easily configurable via the Sharing panel in System Preferences.
                var localHostName = (await CommandUtilities.RunCommandAsync("scutil", "--get LocalHostName")).StandardOutput.FirstOrDefault()?.ToLower();
                // TODO - ensure localHostName is valid subdomain
                if (!string.IsNullOrWhiteSpace(localHostName))
                {
                    Output.Verbose($"[{nameof(NetworkUtility.GetFriendlyHostName)}] Using macOS local hostname (configurable via \"System Preferences > Sharing\"): {localHostName}");
                    return localHostName;
                }
            }

            var machineName = Environment.MachineName;
            Output.Verbose($"[{nameof(NetworkUtility.GetFriendlyHostName)}] Using general machine name: {machineName}");

            return machineName;
        }

        // public static string GetVpnAccessibleIpAddress()
        // {
        //     NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
        //     foreach (NetworkInterface adapter in nics)
        //     {
        //         foreach (var x in adapter.GetIPProperties().UnicastAddresses)
        //         {
        //             if (x.Address.AddressFamily == AddressFamily.InterNetwork && x.IsDnsEligible)
        //             {
        //                 Output.Info($" IPAddress ........ : {x.Address.ToString():x}");
        //             }
        //         }
        //     }
        //     return "NOPE";
        // }
    }
}