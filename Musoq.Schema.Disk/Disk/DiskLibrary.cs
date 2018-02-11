using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Group = Musoq.Plugins.Group;

namespace Musoq.Schema.Disk.Disk
{
    [BindableClass]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class DiskLibrary : LibraryBase
    {
        [BindableMethod]
        public string Sha256File([InjectSource] FileInfo file)
        {
            using (var stream = file.Open(FileMode.Open))
            {
                var sha = new SHA256Managed();
                var hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        [BindableMethod]
        public string Md5File([InjectSource] FileInfo file)
        {
            using (var stream = file.Open(FileMode.Open))
            {
                var sha = new MD5CryptoServiceProvider();
                var hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
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
            using (var stream = new StreamReader(file.Open(FileMode.Open)))
            {
                var content = stream.ReadToEnd();
                return Like(content, expression);
            }
        }

        [BindableMethod]
        public bool NotLike(string content, string expression)
        {
            return !Like(content, expression);
        }

        [BindableMethod]
        public bool HasAttribute([InjectSource] FileInfo file, long flags)
        {
            return (flags & Convert.ToUInt32(file.Attributes)) == flags;
        }

        [BindableMethod]
        public string GetLinesContainingWord([InjectSource] FileInfo file, string word)
        {
            using (var stream = new StreamReader(file.Open(FileMode.Open)))
            {
                var lines = new List<string>();
                var line = 1;
                while (!stream.EndOfStream)
                {
                    if (stream.ReadLine().Contains(word))
                        lines.Add(line.ToString());
                    line += 1;
                }

                var builder = new StringBuilder('(');

                for (int i = 0, j = lines.Count - 1; i < j; ++i) builder.Append(lines[i]);

                builder.Append(lines[lines.Count]);
                builder.Append(')');

                return builder.ToString();
            }
        }

        [BindableMethod]
        public string Substring(string content, int startIndex, int length)
        {
            return content.Substring(startIndex, length);
        }

        [BindableMethod]
        public long Format([InjectSource] FileInfo context, string unit = "b")
        {
            switch (unit)
            {
                case "b":
                    return context.Length;
                case "kb":
                    return Convert.ToInt64(context.Length / 1024f);
                case "mb":
                    return Convert.ToInt64(context.Length / 1024f / 1024f);
                case "gb":
                    return Convert.ToInt64(context.Length / 1024f / 1024f / 1024f);
                default:
                    throw new NotSupportedException();
            }
        }

        [BindableMethod]
        public long CountOfLines([InjectSource] FileInfo context)
        {
            using (var stream = new StreamReader(context.Open(FileMode.Open)))
            {
                var lines = 0;
                while (!stream.EndOfStream)
                {
                    lines += 1;
                    stream.ReadLine();
                }

                return lines;
            }
        }

        [BindableMethod]
        public long CountOfNotEmptyLines([InjectSource] FileInfo context)
        {
            using (var stream = new StreamReader(context.Open(FileMode.Open)))
            {
                var lines = 0;
                while (!stream.EndOfStream)
                {
                    var line = stream.ReadLine();

                    if (line == string.Empty)
                        continue;

                    lines += 1;
                }

                return lines;
            }
        }

        [AggregationSetMethod]
        public void SetAggregateFiles([InjectGroup] Group group, [InjectSource] FileInfo file, string name)
        {
            var list = group.GetOrCreateValue(name, new List<FileInfo>());

            list.Add(file);
        }

        [AggregationGetMethod]
        public IReadOnlyList<FileInfo> AggregateFiles([InjectGroup] Group group, string name)
        {
            return group.GetValue<IReadOnlyList<FileInfo>>(name);
        }

        [BindableMethod]
        public string Compress(IReadOnlyList<FileInfo> files, string path, string method)
        {
            if (files.Count == 0)
                return string.Empty;

            CompressionLevel level;
            switch (method.ToLowerInvariant())
            {
                case "fastest":
                    level = CompressionLevel.Fastest;
                    break;
                case "optimal":
                    level = CompressionLevel.Optimal;
                    break;
                case "max":
                    level = CompressionLevel.NoCompression;
                    break;
                default:
                    throw new NotSupportedException(method);
            }

            var operationSucessfull = true;
            using (var zipArchiveFile = File.Open(path, FileMode.OpenOrCreate))
            {
                try
                {
                    using (var zip = new ZipArchive(zipArchiveFile, ZipArchiveMode.Create))
                    {
                        foreach (var file in files)
                        {
                            zip.CreateEntryFromFile(file.FullName, file.Name, level);
                        }
                    }
                }
                catch (Exception e)
                {
                    operationSucessfull = false;
                }
            }

            return operationSucessfull ? path : string.Empty;
        }
    }
}