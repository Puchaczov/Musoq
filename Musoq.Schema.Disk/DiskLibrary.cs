using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Group = Musoq.Plugins.Group;

namespace Musoq.Schema.Disk
{
    [BindableClass]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public partial class DiskLibrary : LibraryBase
    {
        [BindableMethod]
        public string Sha1File([InjectSource] FileInfo file)
        {
            using (var stream = file.OpenRead())
            {
                var sha = new SHA1CryptoServiceProvider();
                var hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        [BindableMethod]
        public string Sha256File([InjectSource] FileInfo file)
        {
            using (var stream = file.OpenRead())
            {
                var sha = new SHA256Managed();
                var hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        [BindableMethod]
        public string Md5File([InjectSource] FileInfo file)
        {
            using (var stream = file.OpenRead())
            {
                var sha = new MD5CryptoServiceProvider();
                var hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        [BindableMethod]
        public bool HasContent([InjectSource] FileInfo file, string pattern)
        {
            using (var stream = new StreamReader(file.OpenRead()))
            {
                var content = stream.ReadToEnd();
                return Regex.IsMatch(content, pattern);
            }
        }

        [BindableMethod]
        public bool Like([InjectSource] FileInfo file, string expression)
        {
            using (var stream = new StreamReader(file.OpenRead()))
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
            using (var stream = new StreamReader(file.OpenRead()))
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
            using (var stream = new StreamReader(context.OpenRead()))
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
            using (var stream = new StreamReader(context.OpenRead()))
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
        public void SetAggregateFiles([InjectGroup] Group group, string name, FileInfo file)
        {
            var list = group.GetOrCreateValue(name, new List<FileInfo>());

            list.Add(file);
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

        [AggregationSetMethod]
        public void SetAggregateDirectories([InjectGroup] Group group, [InjectSource] DirectoryInfo directory, string name)
        {
            var list = group.GetOrCreateValue(name, new List<DirectoryInfo>());

            list.Add(directory);
        }

        [AggregationGetMethod]
        public IReadOnlyList<DirectoryInfo> AggregateDirectories([InjectGroup] Group group, string name)
        {
            return group.GetValue<IReadOnlyList<DirectoryInfo>>(name);
        }

        private class DirectoryinfoPosition
        {
            public DirectoryinfoPosition(DirectoryInfo dir, bool isRoot)
            {
                Directory = dir;
                IsRoot = isRoot;
            }

            public DirectoryInfo Directory { get; }
            public bool IsRoot { get; }
        }

        [BindableMethod]
        public string Compress(IReadOnlyList<DirectoryInfo> directories, string path, string method)
        {
            if (directories.Count == 0)
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
                case "nocompression":
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
                        var dirs = new Stack<DirectoryinfoPosition>();

                        foreach (var dir in directories)
                            dirs.Push(new DirectoryinfoPosition(dir, true));

                        while (dirs.Count > 0)
                        {
                            var dir = dirs.Pop();

                            DirectoryInfo root = null;

                            if (dir.IsRoot)
                                root = dir.Directory.Parent;

                            foreach (var file in dir.Directory.GetFiles())
                            {
                                var entryName = file.FullName.Substring(root.FullName.Length);
                                zip.CreateEntryFromFile(file.FullName, entryName.Trim('\\'), level);
                            }

                            foreach(var subDir in dir.Directory.GetDirectories())
                                dirs.Push(new DirectoryinfoPosition(subDir, false));
                        }
                    }
                }
                catch (Exception)
                {
                    operationSucessfull = false;
                }
            }

            return operationSucessfull ? path : string.Empty;
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
                case "nocompression":
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
                catch (Exception)
                {
                    operationSucessfull = false;
                }
            }

            return operationSucessfull ? path : string.Empty;
        }

        [BindableMethod]
        public string Decompress(IReadOnlyList<FileInfo> files, string path)
        {
            if (files.Count == 0)
                return string.Empty;

            var operationSucessfull = true;

            try
            {
                var tempPath = Path.GetTempPath();
                var groupedByDir = files.GroupBy(f => f.DirectoryName.Substring(tempPath.Length)).ToArray();
                tempPath = tempPath.TrimEnd('\\');

                var di = new DirectoryInfo(tempPath);
                var rootDirs = di.GetDirectories();
                foreach (var dir in groupedByDir)
                {
                    if (rootDirs.All(f => dir.Key.Contains('\\') || !f.FullName.Substring(tempPath.Length).EndsWith(dir.Key)))
                        continue;

                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    var extractedDirs = new Stack<DirectoryInfo>();
                    extractedDirs.Push(new DirectoryInfo(Path.Combine(tempPath, dir.Key)));

                    while (extractedDirs.Count > 0)
                    {
                        var extractedDir = extractedDirs.Pop();

                        foreach (var file in extractedDir.GetFiles())
                        {
                            var subDir = string.Empty;
                            var pUri = new Uri(extractedDir.Parent.FullName);
                            var tUri = new Uri(tempPath);
                            if (pUri != tUri)
                                subDir = extractedDir.FullName.Substring(tempPath.Length).Replace(dir.Key, string.Empty).TrimStart('\\');

                            var destDir = Path.Combine(path, subDir);
                            var destPath = Path.Combine(destDir, file.Name);

                            if (!Directory.Exists(destDir))
                                Directory.CreateDirectory(destDir);

                            if (File.Exists(destPath))
                                File.Delete(destPath);

                            File.Move(file.FullName, destPath);
                        }

                        foreach (var subDir in extractedDir.GetDirectories())
                        {
                            extractedDirs.Push(subDir);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                operationSucessfull = false;
            }

            return operationSucessfull ? path : string.Empty;
        }
    }
}