using System;
using System.IO;

namespace NugetPushIssueRepro.Utility
{
    internal class PhysicalFileSystem : IFileSystem
    {
        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }

        public Stream OpenRead(string path)
        {
            return File.OpenRead(path);
        }

        public void WriteAllText(string path, string text)
        {
            File.WriteAllText(path, text);
        }
    }
}