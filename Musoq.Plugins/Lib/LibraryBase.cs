using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Musoq.Plugins.Attributes;
using Musoq.Plugins.Helpers;

namespace Musoq.Plugins
{
    [BindableClass]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    public abstract partial class LibraryBase : UserMethodsLibrary
    {
        [BindableMethod]
        public int RowNumber([InjectQueryStats] QueryStats info)
        {
            return info.RowNumber;
        }

        [BindableMethod]
        public string Sha512(string content)
        {
            if (content == null)
                return null;

            return HashHelper.ComputeHash<SHA512Managed>(content);
        }

        [BindableMethod]
        public string Sha256(string content)
        {
            if (content == null)
                return null;

            return HashHelper.ComputeHash<SHA256Managed>(content);
        }

        [BindableMethod]
        public string Md5(string content)
        {
            if (content == null)
                return null;

            return HashHelper.ComputeHash<MD5CryptoServiceProvider>(content);
        }
    }
}