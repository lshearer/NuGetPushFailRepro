using System;

namespace NugetPushIssueRepro.Utility
{
    internal interface IConsole
    {
        void WriteLine(string message, Exception exception, ConsoleWriteOptions options);
    }
}