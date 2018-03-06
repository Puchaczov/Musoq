using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Musoq.Plugins.Helpers
{
    public static class HashHelper
    {
        public static string ComputeHash<THashProvider>(string content)
            where THashProvider : HashAlgorithm, new()
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                return ComputeHash<THashProvider>(stream);
            }
        }

        public static string ComputeHash<THashProvider>(Stream stream)
            where THashProvider : HashAlgorithm, new()
        {
            var sha = new THashProvider();
            var hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }
}
