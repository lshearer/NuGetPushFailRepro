using System;

namespace NugetPushIssueRepro.Utility
{
    internal static class ValidationExtensions
    {
        public static TValue ThrowIfNull<TValue>(this TValue value, string parameterName)
            where TValue : class
        {
            if (value is null)
            {
                throw new ArgumentNullException(parameterName);
            }
            return value;
        }

        public static string ThrowIfNullOrEmpty(this string value, string parameterName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"{parameterName} cannot be null or empty.", parameterName);
            }
            return value;
        }

        public static string ThrowIfNullOrWhiteSpace(this string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{parameterName} cannot be null or whitespace.", parameterName);
            }
            return value;
        }
    }
}