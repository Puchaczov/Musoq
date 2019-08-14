using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Plugins.Helpers;
using Group = Musoq.Plugins.Group;

namespace Musoq.Schema.Os
{
    [BindableClass]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class OsLibrary : LibraryBase
    {
        [BindableMethod]
        public byte[] Head([InjectSource] FileInfo file, int length)
            => GetFileBytes(file, length);

        [BindableMethod]
        public byte[] Tail([InjectSource] FileInfo file, int length)
        {
            using (var stream = file.OpenRead())
            using (var reader = new BinaryReader(stream))
            {
                var toRead = length < stream.Length ? length : stream.Length;

                var bytes = new byte[toRead];

                stream.Position = stream.Length - length;
                for (var i = 0; i < toRead; ++i)
                    bytes[i] = reader.ReadByte();

                return bytes;
            }
        }

        [BindableMethod]
        public byte[] GetFileBytes([InjectSource] FileInfo file, long bytesCount = long.MaxValue)
        {
            using (var stream = file.OpenRead())
            using (var reader = new BinaryReader(stream))
            {
                var toRead = bytesCount < stream.Length ? bytesCount : stream.Length;

                var bytes = new byte[toRead];

                for (var i = 0; i < toRead; ++i)
                    bytes[i] = reader.ReadByte();

                return bytes;
            }
        }

        [BindableMethod]
        public string Sha1File([InjectSource] FileInfo file)
        {
            using (var stream = file.OpenRead())
            {
                return HashHelper.ComputeHash<SHA1CryptoServiceProvider>(stream);
            }
        }

        [BindableMethod]
        public string Sha256File([InjectSource] FileInfo file)
        {
            using (var stream = file.OpenRead())
            {
                return HashHelper.ComputeHash<SHA256CryptoServiceProvider>(stream);
            }
        }

        [BindableMethod]
        public string Md5File([InjectSource] FileInfo file)
        {
            using (var stream = file.OpenRead())
            {
                return HashHelper.ComputeHash<MD5CryptoServiceProvider>(stream);
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
                    var strLine = stream.ReadLine();
                    if (strLine != null && strLine.Contains(word))
                        lines.Add(line.ToString());
                    line += 1;
                }

                var builder = new StringBuilder("(");

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
            switch (unit.ToLowerInvariant())
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
                    throw new NotSupportedException($"unsupported unit ({unit})");
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
        public void SetAggregateDirectories([InjectGroup] Group group, [InjectSource] DirectoryInfo directory,
            string name)
        {
            var list = group.GetOrCreateValue(name, new List<DirectoryInfo>());

            list.Add(directory);
        }

        [AggregationGetMethod]
        public IReadOnlyList<DirectoryInfo> AggregateDirectories([InjectGroup] Group group, string name)
        {
            return group.GetValue<IReadOnlyList<DirectoryInfo>>(name);
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
                            dirs.Push(new DirectoryinfoPosition(dir, dir.Parent));

                        while (dirs.Count > 0)
                        {
                            var dir = dirs.Pop();

                            foreach (var file in dir.Directory.GetFiles())
                            {
                                var entryName = file.FullName.Substring(dir.RootDirectory.FullName.Length);
                                zip.CreateEntryFromFile(file.FullName, entryName.Trim('\\'), level);
                            }

                            foreach (var subDir in dir.Directory.GetDirectories())
                                dirs.Push(new DirectoryinfoPosition(subDir, dir.RootDirectory));
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
                        foreach (var file in files) zip.CreateEntryFromFile(file.FullName, file.Name, level);
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
                    if (rootDirs.All(f =>
                        dir.Key.Contains('\\') || !f.FullName.Substring(tempPath.Length).EndsWith(dir.Key)))
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
                                subDir = extractedDir.FullName.Substring(tempPath.Length).Replace(dir.Key, string.Empty)
                                    .TrimStart('\\');

                            var destDir = Path.Combine(path, subDir);
                            var destPath = Path.Combine(destDir, file.Name);

                            if (!Directory.Exists(destDir))
                                Directory.CreateDirectory(destDir);

                            if (File.Exists(destPath))
                                File.Delete(destPath);

                            File.Move(file.FullName, destPath);
                        }

                        foreach (var subDir in extractedDir.GetDirectories()) extractedDirs.Push(subDir);
                    }
                }
            }
            catch (Exception e)
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                else
                    Debug.WriteLine(e);

                operationSucessfull = false;
            }

            return operationSucessfull ? path : string.Empty;
        }

        [BindableMethod]
        public string Combine(string path1, string path2)
        {
            return Path.Combine(path1, path2);
        }

        [BindableMethod]
        public string Combine(string path1, string path2, string path3)
        {
            return Path.Combine(path1, path2, path3);
        }

        [BindableMethod]
        public string Combine(string path1, string path2, string path3, string path4)
        {
            return Path.Combine(path1, path2, path3, path4);
        }

        [BindableMethod]
        public string Combine(string path1, string path2, string path3, string path4, string path5)
        {
            return Path.Combine(path1, path2, path3, path4, path5);
        }

        [BindableMethod]
        public string Combine(params string[] paths)
        {
            return Path.Combine(paths);
        }

        private class DirectoryinfoPosition
        {
            public DirectoryinfoPosition(DirectoryInfo dir, DirectoryInfo root)
            {
                Directory = dir;
                RootDirectory = root;
            }

            public DirectoryInfo Directory { get; }

            public DirectoryInfo RootDirectory { get; }
        }
    }
}