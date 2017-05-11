using System;

namespace NugetPushIssueRepro.Utility
{
    internal class ConsoleWriteOptions
    {
        public ConsoleColor Color { get; set; }
        public bool UseErrorStream { get; set; } = false;

        public LogLevel Level { get; set; }

        public enum LogLevel
        {
            Debug = 1,
            Info = 2,
            Warn = 3,
            Error = 4,
        }
    }
}