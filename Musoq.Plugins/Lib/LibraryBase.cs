using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Musoq.Plugins.Attributes;
using Musoq.Plugins.Helpers;

namespace Musoq.Plugins
{
    /// <summary>
    /// Library base type that all other types should inherit from.
    /// </summary>
    [BindableClass]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    public partial class LibraryBase : UserMethodsLibrary
    {
        /// <summary>
        /// Gets the row number of the current row.
        /// </summary>
        /// <param name="info" injectedByRuntime="true">The queryStats object</param>
        /// <returns>The row number</returns>
        [BindableMethod]
        public int RowNumber([InjectQueryStats] QueryStats info)
        {
            return info.RowNumber;
        }

        /// <summary>
        /// Gets the sha256 hash of the given string.
        /// </summary>
        /// <param name="content">The content string</param>
        /// <returns>The sha256 value</returns>
        [BindableMethod]
        public string Sha512(string content)
        {
            if (content == null)
                return null;

            return HashHelper.ComputeHash<SHA512Managed>(content);
        }

        /// <summary>
        /// Gets the sha256 hash of the given string.
        /// </summary>
        /// <param name="content">The content string</param>
        /// <returns>The sha256 value</returns>
        [BindableMethod]
        public string Sha256(string content)
        {
            if (content == null)
                return null;

            return HashHelper.ComputeHash<SHA256Managed>(content);
        }

        /// <summary>
        /// Gets the md5 hash of the given string.
        /// </summary>
        /// <param name="content">The content string</param>
        /// <returns>The sha256 value</returns>
        [BindableMethod]
        public string Md5(string content)
        {
            if (content == null)
                return null;

            return HashHelper.ComputeHash<MD5CryptoServiceProvider>(content);
        }

        /// <summary>
        /// Gets the typename of passed object.
        /// </summary>
        /// <param name="obj">Object of unknown type that the typename have to be retrieved</param>
        /// <returns>The typename value</returns>
        [BindableMethod]
        public string GetTypeName(object obj)
        {
            return obj?.GetType().FullName;
        }
    }
}