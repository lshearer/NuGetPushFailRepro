using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NugetPushIssueRepro
{
    internal static class LabelUtilities
    {
        public static List<string> FormatLabelsAsArguments(Dictionary<string, string> labels)
        {
            var formattedLabels = labels.Select(label =>
            {
                ValidateLabelKey(label.Key);
                ValidateLabelValue(label.Value);
                return $"--label {label.Key}={label.Value}";
            });
            return formattedLabels.ToList();
        }


        // Docker actually says there shouldn't be underscores, but it allows them, so we'll allow them for now at least.
        // https://docs.docker.com/engine/userguide/labels-custom-metadata/#/key-format-recommendations
        private static readonly Regex LabelKeyRegex = new Regex("^[a-zA-Z0-9._-]+$");

        internal static void ValidateLabelKey(string key)
        {
            if (!LabelKeyRegex.IsMatch(key))
            {
                throw new FormatException($"Image label key does not match expected format. Key={key} ExpectedFormat={LabelKeyRegex.ToString()}");
            }
        }

        // Ensure no whitespace to prevent issues with CLI args
        // TODO exclude other values as well? E.g., `>` or other shell special characters. Otherwise these may need escaped in a cross-platform fashion.
        // https://docs.docker.com/engine/userguide/labels-custom-metadata/#/value-guidelines
        private static readonly Regex LabelValueRegex = new Regex(@"^\S+$");

        internal static void ValidateLabelValue(string value)
        {
            if (!LabelValueRegex.IsMatch(value))
            {
                throw new FormatException($"Image label value does not match expected format. Value={value} ExpectedFormat={LabelValueRegex.ToString()}");
            }
        }

    }
}