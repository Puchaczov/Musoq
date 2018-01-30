using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FQL.Evaluator;
using FQL.Plugins;
using FQL.Plugins.Attributes;

namespace FQL.Examples
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class StandardLibrary : LibraryBase
    {
        [BindableMethod]
        public string Sha1File([InjectSource] FileInfo file)
        {
            using (var stream = file.Open(FileMode.Open))
            {
                var sha = new SHA256Managed();
                var hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", String.Empty);
            }
        }

        [BindableMethod]
        public string Md5File([InjectSource] FileInfo file)
        {
            using (var stream = file.Open(FileMode.Open))
            {
                var sha = new MD5CryptoServiceProvider();
                var hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", String.Empty);
            }
        }

        [BindableMethod]
        public string Sha1(string content)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                var sha = new SHA256Managed();
                var hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", String.Empty);
            }
        }

        [BindableMethod]
        public string Md5(string content)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                var sha = new MD5CryptoServiceProvider();
                var hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", String.Empty);
            }
        }

        [BindableMethod]
        public bool HasContent([InjectSource] FileInfo file, string pattern)
        {
            using (var stream = new StreamReader(file.Open(FileMode.Open)))
            {
                var content = stream.ReadToEnd();
                return Regex.IsMatch(content, pattern);
            }
        }

        [BindableMethod]
        public bool Like([InjectSource] FileInfo file, string expression)
        {
            return false;
        }

        [BindableMethod]
        public string GetLinesContainingWord([InjectSource] FileInfo file, string word)
        {
            using (var stream = new StreamReader(file.Open(FileMode.Open)))
            {
                List<string> lines = new List<string>();
                var line = 1;
                while (!stream.EndOfStream)
                {
                    if (stream.ReadLine().Contains(word))
                        lines.Add(line.ToString());
                    line += 1;
                }

                StringBuilder builder = new StringBuilder('(');

                for (int i = 0, j = lines.Count - 1; i < j; ++i)
                {
                    builder.Append(lines[i]);
                }

                builder.Append(lines[lines.Count]);
                builder.Append(')');

                return builder.ToString();
            }
        }

        [BindableMethod]
        public string Concat(string first, string second)
        {
            return first + second;
        }

        [BindableMethod]
        public string Substring(string content, int startIndex, int length)
        {
            return content.Substring(startIndex, length);
        }
    }
}