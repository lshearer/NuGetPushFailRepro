using System;

namespace NugetPushIssueRepro.Utility
{
    internal static class ConsoleExtensions
    {
        public static void Info(this IConsole logger, string message)
        {
            logger.WriteLine(message, null, null);
        }

        public static void Error(this IConsole logger, string message, Exception exception = null)
        {
            logger.WriteLine(message, exception, new ConsoleWriteOptions
            {
                Color = ConsoleColor.Red,
                UseErrorStream = true,
            });
        }
    }
}