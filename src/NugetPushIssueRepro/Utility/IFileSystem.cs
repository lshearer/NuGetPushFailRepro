using System.IO;

namespace NugetPushIssueRepro.Utility
{
    internal interface IFileSystem
    {
        void Delete(string path);

        Stream OpenRead(string path);

        void WriteAllText(string path, string text);

        void CreateDirectory(string path);
    }
}