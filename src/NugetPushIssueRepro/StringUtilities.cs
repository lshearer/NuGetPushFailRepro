using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace NugetPushIssueRepro
{
    internal class StringUtilities
    {
        public static string PascalCasedToDasherized(string pascalInput)
        {
            var nameParts = new Regex("([A-Z][a-z]*|[0-9]+)").Matches(pascalInput).OfType<Match>()
                .Select(match => match.Captures[0].Value);
            // Doing a single regex with repeated word parts AND anchoring at the start and end doesn't seem possible, so
            // we'll just re-concatenate the word parts to ensure there aren't extra characters that aren't getting matched
            var reconstructedInput = string.Join("", nameParts);
            if (reconstructedInput != pascalInput)
            {
                throw new FormatException($"Input name doesn't match expected Pascal-cased format. Input={pascalInput}, MatchedParts={reconstructedInput}");
            }

            var lowercasedWords = nameParts.Select(part => part.ToLower());
            return string.Join("-", lowercasedWords);
        }

    }
}