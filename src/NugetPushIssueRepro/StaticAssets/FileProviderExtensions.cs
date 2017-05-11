using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace NugetPushIssueRepro.StaticAssets
{
    internal static class FileProviderExtensions
    {
        public static IEnumerable<IFileInfo> GetFilesRecursive(this IFileProvider fileProvider, string subpath)
        {
            var contents = fileProvider.GetDirectoryContents(subpath);
            var files = new List<IFileInfo>();
            var directories = new List<IFileInfo>();
            foreach (var fileInfo in contents)
            {
                // Do a breadth-first iteration in case
                if (fileInfo.IsDirectory)
                {
                    directories.Add(fileInfo);
                    continue;
                }
                files.Add(fileInfo);
            }

            foreach (var directory in directories)
            {
                var filesForDirectory = fileProvider.GetFilesRecursive(Path.Combine(subpath, directory.Name));
                files.AddRange(filesForDirectory);
            }
            return files;
        }
    }
}