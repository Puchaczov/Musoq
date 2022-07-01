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
using Musoq.Schema.Exceptions;
using Musoq.Schema.Os.Files;
using Musoq.Schema.Os.Zip;
using Group = Musoq.Plugins.Group;

namespace Musoq.Schema.Os
{
    [BindableClass]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class OsLibrary : LibraryBase
    {
        private static readonly HashSet<string> IsZipArchiveSet = new()
        {
            ".zip",
            ".jar",
            ".war",
            ".ear"
        };

        private static readonly HashSet<string> IsArchiveSet = new()
        {
            ".7z",
            ".bz2",
            ".bzip2",
            ".gzip",
            ".lz",
            ".rar",
            ".tar",
            ".xz",
            ".zip"
        };

        private static readonly HashSet<string> IsAudioSet = new()
        {
            ".aac",
            ".aiff",
            ".amr",
            ".flac",
            ".gsm",
            ".m4a",
            ".m4b",
            ".m4p",
            ".mp3",
            ".ogg",
            ".wma",
            ".aa", 
            ".aax", 
            ".ape", 
            ".dsf", 
            ".mpc", 
            ".mpp", 
            ".oga", 
            ".wav", 
            ".wv", 
            ".webm"
        };

        private static readonly HashSet<string> IsBookSet = new()
        {
            ".azw3",
            ".chm",
            ".djvu",
            ".epub",
            ".fb2",
            ".mobi",
            ".pdf"
        };

        private static readonly HashSet<string> IsDocSet = new()
        {
            ".accdb",
            ".doc",
            ".docm",
            ".docx",
            ".dot",
            ".dotm",
            ".dotx",
            ".mdb",
            ".ods",
            ".odt",
            ".pdf",
            ".potm",
            ".potx",
            ".ppt",
            ".pptm",
            ".pptx",
            ".rtf",
            ".xlm",
            ".xls",
            ".xlsm",
            ".xlsx",
            ".xlt",
            ".xltm",
            ".xltx",
            ".xps"
        };

        private static readonly HashSet<string> IsImageSet = new()
        {
            ".bmp",
            ".gif",
            ".jpeg",
            ".jpg",
            ".png",
            ".psb",
            ".psd",
            ".tiff",
            ".webp",
            ".pbm", 
            ".pgm", 
            ".ppm", 
            ".pnm", 
            ".pcx", 
            ".dng", 
            ".svg"
        };

        private static readonly HashSet<string> IsSourceSet = new()
        {
            ".asm",
            ".bas",
            ".c",
            ".cc",
            ".ceylon",
            ".clj",
            ".coffee",
            ".cpp",
            ".cs",
            ".dart",
            ".elm",
            ".erl",
            ".go",
            ".groovy",
            ".h",
            ".hh",
            ".hpp",
            ".java",
            ".js",
            ".jsp",
            ".kt",
            ".kts",
            ".lua",
            ".nim",
            ".pas",
            ".php",
            ".pl",
            ".pm",
            ".py",
            ".rb",
            ".rs",
            ".scala",
            ".swift",
            ".tcl",
            ".vala",
            ".vb"
        };

        private static readonly HashSet<string> IsVideoSet = new()
        {
            ".3gp",
            ".avi",
            ".flv",
            ".m4p",
            ".m4v",
            ".mkv",
            ".mov",
            ".mp4",
            ".mpeg",
            ".mpg",
            ".webm",
            ".wmv",
            ".ogv", 
            ".asf", 
            ".mpe", 
            ".mpv", 
            ".m2v"
        };

        [BindableMethod]
        public bool IsZipArchive(string extension) => IsZipArchiveSet.Contains(extension);

        [BindableMethod]
        public bool IsZipArchive([InjectSource] ExtendedFileInfo fileInfo) => IsZipArchiveSet.Contains(fileInfo.Extension);

        [BindableMethod]
        public bool IsArchive(string extension) => IsArchiveSet.Contains(extension);

        [BindableMethod]
        public bool IsArchive([InjectSource] ExtendedFileInfo fileInfo) => IsArchiveSet.Contains(fileInfo.Extension);

        [BindableMethod]
        public bool IsAudio(string extension) => IsAudioSet.Contains(extension);

        [BindableMethod]
        public bool IsAudio([InjectSource] ExtendedFileInfo fileInfo) => IsAudioSet.Contains(fileInfo.Extension);

        [BindableMethod]
        public bool IsBook(string extension) => IsBookSet.Contains(extension);

        [BindableMethod]
        public bool IsBook([InjectSource] ExtendedFileInfo fileInfo) => IsBookSet.Contains(fileInfo.Extension);

        [BindableMethod]
        public bool IsDoc(string extension) => IsDocSet.Contains(extension);

        [BindableMethod]
        public bool IsDoc([InjectSource] ExtendedFileInfo fileInfo) => IsDocSet.Contains(fileInfo.Extension);

        [BindableMethod]
        public bool IsImage(string extension) => IsImageSet.Contains(extension);

        [BindableMethod]
        public bool IsImage([InjectSource] ExtendedFileInfo fileInfo) => IsImageSet.Contains(fileInfo.Extension);

        [BindableMethod]
        public bool IsSource(string extension) => IsSourceSet.Contains(extension);

        [BindableMethod]
        public bool IsSource([InjectSource] ExtendedFileInfo fileInfo) => IsSourceSet.Contains(fileInfo.Extension);

        [BindableMethod]
        public bool IsVideo(string extension) => IsVideoSet.Contains(extension);

        [BindableMethod]
        public bool IsVideo([InjectSource] ExtendedFileInfo fileInfo) => IsVideoSet.Contains(fileInfo.Extension);

        [BindableMethod]
        public string GetFileContent([InjectSource] ExtendedFileInfo extendedFileInfo)
        {
            if (!extendedFileInfo.Exists)
                return null;

            using (var file = extendedFileInfo.OpenRead())
            using (var fileReader = new StreamReader(file))
            {
                return fileReader.ReadToEnd();
            }
        }

        [BindableMethod]
        public string GetRelativePath([InjectSource] ExtendedFileInfo fileInfo)
        {
            if (fileInfo == null)
                return null;

            return fileInfo.FullName.Replace(fileInfo.ComputationRootDirectoryPath, string.Empty);
        }

        [BindableMethod]
        public string GetRelativePath([InjectSource] ExtendedFileInfo fileInfo, string basePath)
        {
            if (fileInfo == null)
                return null;

            if (basePath == null)
                throw new ArgumentNullException(nameof(basePath));

            if (!Directory.Exists(basePath))
                throw new DirectoryNotFoundException(basePath);

            basePath = new DirectoryInfo(basePath).FullName;

            return fileInfo.FullName.Replace(basePath, string.Empty);
        }

        [BindableMethod]
        public byte[] Head([InjectSource] ExtendedFileInfo file, int length)
            => GetFileBytes(file, length, 0);

        [BindableMethod]
        public byte[] Tail([InjectSource] ExtendedFileInfo file, int length)
        {
            if (file == null)
                throw new InjectSourceNullReferenceException(typeof(FileInfo));

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
        public byte[] GetFileBytes([InjectSource] ExtendedFileInfo file, long bytesCount = long.MaxValue, long offset = 0)
        {
            if (file == null)
                throw new InjectSourceNullReferenceException(typeof(FileInfo));

            using (var stream = file.OpenRead())
            using (var reader = new BinaryReader(stream))
            {
                if (offset > 0)
                    stream.Seek(offset, SeekOrigin.Begin);

                var toRead = bytesCount < stream.Length ? bytesCount : stream.Length;

                var bytes = new byte[toRead];

                for (var i = 0; i < toRead; ++i)
                    bytes[i] = reader.ReadByte();

                return bytes;
            }
        }

        [BindableMethod]
        public string Sha1File([InjectSource] ExtendedFileInfo file)
        {
            if (file == null)
                throw new InjectSourceNullReferenceException(typeof(ExtendedFileInfo));

            using (var stream = file.OpenRead())
            {
                return HashHelper.ComputeHash<SHA1CryptoServiceProvider>(stream);
            }
        }

        [BindableMethod]
        public string Sha256File([InjectSource] ExtendedFileInfo file)
        {
            if (file == null)
                throw new InjectSourceNullReferenceException(typeof(ExtendedFileInfo));

            return Sha256File(file.FileInfo);
        }

        public string Sha256File(FileInfo file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            using (var stream = file.OpenRead())
            {
                return HashHelper.ComputeHash<SHA256CryptoServiceProvider>(stream);
            }
        }

        [BindableMethod]
        public string Md5File([InjectSource] ExtendedFileInfo file)
        {
            if (file == null)
                throw new InjectSourceNullReferenceException(typeof(ExtendedFileInfo));

            using (var stream = file.OpenRead())
            {
                return HashHelper.ComputeHash<MD5CryptoServiceProvider>(stream);
            }
        }

        [BindableMethod]
        public bool HasContent([InjectSource] ExtendedFileInfo file, string pattern)
        {
            if (file == null)
                throw new InjectSourceNullReferenceException(typeof(ExtendedFileInfo));

            using (var stream = new StreamReader(file.OpenRead()))
            {
                var content = stream.ReadToEnd();
                return Regex.IsMatch(content, pattern);
            }
        }

        [BindableMethod]
        public bool HasAttribute([InjectSource] ExtendedFileInfo file, long flags)
        {
            return (flags & Convert.ToUInt32(file.Attributes)) == flags;
        }

        [BindableMethod]
        public string GetLinesContainingWord([InjectSource] ExtendedFileInfo file, string word)
        {
            if (file == null)
                throw new InjectSourceNullReferenceException(typeof(ExtendedFileInfo));

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
        public long GetFileLength([InjectSource] ExtendedFileInfo context, string unit = "b")
            => GetLengthOfFile(context, unit);

        [BindableMethod]
        public long GetLengthOfFile([InjectSource] ExtendedFileInfo context, string unit = "b")
        {
            if (context == null)
                throw new InjectSourceNullReferenceException(typeof(ExtendedFileInfo));

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
        public string SubPath([InjectSource] DirectoryInfo context, int nesting)
            => SubPath(context.FullName, nesting);

        [BindableMethod]
        public string SubPath([InjectSource] ExtendedFileInfo context, int nesting)
            => SubPath(context.Directory.FullName, nesting);

        [BindableMethod]
        public string RelativeSubPath([InjectSource] ExtendedFileInfo context, int nesting)
            => SubPath(GetRelativePath(context, context.ComputationRootDirectoryPath), nesting);
        
        [BindableMethod]
        public string SubPath(string directoryPath, int nesting)
        {
            if (directoryPath == null)
                return null;

            if (directoryPath == string.Empty)
                return null;

            if (nesting < 0)
                return string.Empty;

            var splittedDirs = directoryPath.Split(Path.DirectorySeparatorChar);
            var subPathBuilder = new StringBuilder();

            subPathBuilder.Append(splittedDirs[0]);

            if (nesting >= 1 && splittedDirs.Length > 1)
            {
                subPathBuilder.Append(Path.DirectorySeparatorChar);

                for (int i = 1; i < nesting && i < splittedDirs.Length - 1; ++i)
                {
                    subPathBuilder.Append(splittedDirs[i]);
                    subPathBuilder.Append(Path.DirectorySeparatorChar);
                }

                subPathBuilder.Append(splittedDirs[nesting < splittedDirs.Length - 1 ? nesting : splittedDirs.Length - 1]);
            }

            return subPathBuilder.ToString();
        }

        [BindableMethod]
        public long Length([InjectSource] ExtendedFileInfo context, string unit = "b")
            => GetLengthOfFile(context, unit);

        [BindableMethod]
        public ExtendedFileInfo GetFileInfo(string fullPath)
        {
            var fileInfo = new FileInfo(fullPath);
            return new ExtendedFileInfo(fileInfo, fileInfo.DirectoryName);
        }

        [BindableMethod]
        public ExtendedFileInfo GetExtendedFileInfo([InjectSource] ExtendedFileInfo context)
            => context;

        [BindableMethod]
        public ExtendedFileInfo GetZipEntryFileInfo([InjectSource] ZipArchiveEntry zipArchiveEntry)
        {
            var fileInfo = SchemaZipHelper.UnpackZipEntry(zipArchiveEntry, zipArchiveEntry.FullName, Path.GetTempPath());
            return new ExtendedFileInfo(fileInfo, fileInfo.DirectoryName);
        }

        [BindableMethod]
        public long CountOfLines([InjectSource] ExtendedFileInfo context)
        {
            if (context == null)
                throw new InjectSourceNullReferenceException(typeof(ExtendedFileInfo));

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
        public long CountOfNotEmptyLines([InjectSource] ExtendedFileInfo context)
        {
            if (context == null)
                throw new InjectSourceNullReferenceException(typeof(ExtendedFileInfo));

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
        public void SetAggregateFiles([InjectGroup] Group group, string name, ExtendedFileInfo file)
        {
            var list = group.GetOrCreateValue(name, new List<ExtendedFileInfo>());

            list.Add(file);
        }

        [AggregationSetMethod]
        public void SetAggregateFiles([InjectGroup] Group group, [InjectSource] ExtendedFileInfo file, string name)
        {
            var list = group.GetOrCreateValue(name, new List<ExtendedFileInfo>());

            list.Add(file);
        }

        [AggregationGetMethod]
        public IReadOnlyList<ExtendedFileInfo> AggregateFiles([InjectGroup] Group group, string name)
        {
            return group.GetValue<IReadOnlyList<ExtendedFileInfo>>(name);
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
        public string Compress(IReadOnlyList<ExtendedFileInfo> files, string path, string method)
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
        public string Decompress(IReadOnlyList<ExtendedFileInfo> files, string path)
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

        [BindableMethod]
        public ZipArchiveEntry GetZipArchiveEntry([InjectSource] ZipArchiveEntry zipArchiveEntry)
        {
            return zipArchiveEntry;
        }

        [BindableMethod]
        public string UnpackTo([InjectSource] ZipArchiveEntry zipArchiveEntry, string destinationDirectory)
        {
            var fileInfo = SchemaZipHelper.UnpackZipEntry(zipArchiveEntry, zipArchiveEntry.FullName, destinationDirectory);
            
            return fileInfo.FullName;
        }

        [BindableMethod]
        public string Unpack([InjectSource] ZipArchiveEntry zipArchiveEntry)
        {
            return UnpackTo(zipArchiveEntry, Path.GetTempPath());
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