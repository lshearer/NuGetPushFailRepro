using System;

namespace NugetPushIssueRepro
{
    internal class ConsoleHelper
    {
        internal static string ReadPassword()
        {
            var password = "";
            var info = System.Console.ReadKey(true);
            while (info.Key != ConsoleKey.Enter)
            {
                if (info.Key != ConsoleKey.Backspace)
                {
                    System.Console.Write("*");
                    password += info.KeyChar;
                }
                else if (info.Key == ConsoleKey.Backspace)
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        // Remove one character from the list of password characters.
                        password = password.Substring(0, password.Length - 1);
                        // Get the location of the cursor
                        var pos = System.Console.CursorLeft;
                        // Move the cursor to the left by one character.
                        System.Console.SetCursorPosition(pos - 1, System.Console.CursorTop);
                        // Replace it with space
                        System.Console.Write(" ");
                        // Move the cursor to the left by one character again.
                        System.Console.SetCursorPosition(pos - 1, System.Console.CursorTop);
                    }
                }
                info = System.Console.ReadKey(true);
            }
            // Add a new line because user pressed enter at the end of their password.
            System.Console.WriteLine();
            return password;
        }

        internal static bool IsInteractive => !System.Console.IsInputRedirected;
    }
}