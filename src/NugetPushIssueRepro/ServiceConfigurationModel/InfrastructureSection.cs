using YamlDotNet.Serialization;
using System.Collections.Generic;

namespace NugetPushIssueRepro.ServiceConfigurationModel
{
        internal class InfrastructureSection
        {
            public class WebServer
            {
                public WebServer()
                {
                    Routes = new List<string>();
                }

                [YamlMember(Alias = "routes")]
                public List<string> Routes { get; set; }
            }
            [YamlMember(Alias = "web")]
            public WebServer Web { get; set; }
        }
}