using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Musoq.Plugins.Helpers
{
    /// <summary>
    /// This class is used to calculate hashes of a string.
    /// </summary>
    public static class HashHelper
    {
        /// <summary>
        /// Computes the hash of a string using the THashProvider algorithm.
        /// </summary>
        /// <param name="content">The content</param>
        /// <typeparam name="THashProvider">The provider</typeparam>
        /// <returns>Hash of a value</returns>
        public static string ComputeHash<THashProvider>(string content)
            where THashProvider : HashAlgorithm, new()
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            return ComputeHash<THashProvider>(stream);
        }
        
        /// <summary>
        /// Computes the hash of a string using the THashProvider algorithm.
        /// </summary>
        /// <param name="stream">The content</param>
        /// <typeparam name="THashProvider">The provider</typeparam>
        /// <returns>Hash of a value</returns>
        public static string ComputeHash<THashProvider>(Stream stream)
            where THashProvider : HashAlgorithm, new()
        {
            using var hashProvider = new THashProvider();
            var hash = hashProvider.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }
}