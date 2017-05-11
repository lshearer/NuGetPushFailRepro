using System;
using System.Security.Cryptography;
using System.Text;

namespace NugetPushIssueRepro.StaticAssets
{
    internal class Sha256HashUtility : IHashUtility
    {
        public string HashData(string data)
        {
            using (var sha1 = SHA256.Create())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(data ?? ""));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }
    }
}