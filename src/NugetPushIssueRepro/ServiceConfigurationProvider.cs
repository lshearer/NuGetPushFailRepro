using System;
using System.IO;
using YamlDotNet.Serialization;
using NugetPushIssueRepro.ServiceConfigurationModel;

namespace NugetPushIssueRepro
{
    internal class ServiceConfigurationProvider
    {
        private readonly string _configFilePathname;

        public ServiceConfigurationProvider(string configFilePathname)
        {
            _configFilePathname = configFilePathname;
        }


        public ServiceConfigFile Load()
        {
            if (!File.Exists(_configFilePathname))
            {
                throw new FileNotFoundException($"Could not find service config file at {_configFilePathname}.");
            }

            var yaml = File.ReadAllText(_configFilePathname);

            var deserializer = new DeserializerBuilder()
                // Ignore extra properties in the YAML. We only need to model the properties we need to read.
                .IgnoreUnmatchedProperties()
                .Build();

            try
            {
                return deserializer.Deserialize<ServiceConfigFile>(yaml);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to parse service config YAML from {_configFilePathname}. Ensure YAML is formatted correctly.", e);
            }
        }
    }
}