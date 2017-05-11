using System;
using System.Collections.Generic;
using System.Linq;
using NugetPushIssueRepro.Utility;

namespace NugetPushIssueRepro
{
    internal static class Output
    {
        internal static bool UseVerbose = false;
        // private static readonly string BuildImageUrl = "761584570493.dkr.ecr.us-east-1.amazonaws.com/dotnet-library-build:master.20161104-0047.0e7439c.18";
        internal static void Success(string message) => Write(message, ConsoleColor.Green);
        internal static void Info(string message) => Write(message, ConsoleColor.White);
        internal static void Verbose(string message)
        {
            if (UseVerbose)
            {
                UseConsoleColor(ConsoleColor.DarkGray, () => Console.Write("debug: "));
                Write(message, ConsoleColor.DarkGray);
            }
        }

        internal static void VerboseException(Exception e)
        {
            if (UseVerbose)
            {
                UseConsoleColor(ConsoleColor.Gray, () => Console.Write("debug: "));
                Exception(e);
            }
        }

        internal static void Error(string message) => UseConsoleColor(ConsoleColor.Red, () => Console.Error.WriteLine(message));
        internal static void Exception(Exception e) => UseConsoleColor(ConsoleColor.Red, () =>
        {
            Console.Error.WriteLine(e.ToOutputString());
        });
        internal static void CommandExecution(string message) => Write($"> {message}", ConsoleColor.White);
        internal static void ExitCode(string executable, int code) => Write($"> {executable} exit code {code}", (code == 0 ? ConsoleColor.Green : ConsoleColor.Yellow));

        internal static string HorizontalLine(char charToRepeat = '=')
        {
            var width = Console.LargestWindowWidth;
            // Account for non-terminal environments (negative width). Handling anything under 8 to account for odd
            // results and ensure we show a noticeable line.
            if (width < 8)
            {
                width = 80;
            }
            return new String(charToRepeat, width);
        }

        private static void Write(string message, ConsoleColor color)
        {
            UseConsoleColor(color, () => Console.WriteLine(message));
        }

        private static void UseConsoleColor(ConsoleColor color, Action writeToConsole)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            writeToConsole();
            Console.ForegroundColor = originalColor;
        }
    }
}
