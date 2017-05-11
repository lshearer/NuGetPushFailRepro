using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NugetPushIssueRepro.Utility
{
    internal static class ExceptionExtensions
    {
        public static TException WithData<TException>(this TException exception, string key, object value)
            where TException : Exception
        {
            exception.Data[key] = value;
            return exception;
        }

        /// <summary>
        /// Formats an exception suitable for program output.
        /// </summary>
        public static string ToOutputString(this Exception exception)
        {
            var exceptionString = exception.ToString();
            if (!string.IsNullOrWhiteSpace(exception.Message) && exception.Data.Count > 0)
            {
                var indent = "  ";
                var data = string.Join(Environment.NewLine, exception.Data.OfType<DictionaryEntry>().Select(kvp => $"{indent}{indent}{kvp.Key}={kvp.Value}"));
                var dataString = $"{Environment.NewLine}{indent}Data:{Environment.NewLine}{data}";

                // We want to take advantage of the default exception formatting but insert the data values after the exception message.
                var start = exceptionString.IndexOf(exception.Message);
                if (start >= 0)
                {
                    exceptionString = exceptionString.Insert(start += exception.Message.Length, dataString);
                }
            }
            return exceptionString;
        }
    }
}