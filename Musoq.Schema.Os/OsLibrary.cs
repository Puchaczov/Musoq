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

namespace Musoq.Schema.Os
{
    /// <summary>
    /// Operating system schema helper methods
    /// </summary>
    [BindableClass]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public partial class OsLibrary : LibraryBase
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

        /// <summary>
        /// Determines whether the extension is zip archive.
        /// </summary>
        /// <param name="extension">Extension that needs to be examined</param>
        /// <returns><see langword="true" />if the specified extension is zip archive; otherwise, <see langword="false" /></returns>
        [BindableMethod]
        public bool IsZipArchive(string extension) => IsZipArchiveSet.Contains(extension);

        /// <summary>
        /// Determines whether the extension is zip archive.
        /// </summary>
        /// <param name="fileInfo">FileInfo that must be examined whether is zip or not</param>
        /// <returns><see langword="true" />if the specified extension is zip archive; otherwise, <see langword="false" /></returns>
        [BindableMethod]
        public bool IsZipArchive([InjectSource] ExtendedFileInfo fileInfo) => IsZipArchiveSet.Contains(fileInfo.Extension);

        /// <summary>
        /// Determine whether the extension is archive.
        /// </summary>
        /// <param name="extension">The extension</param>
        /// <returns>True if archive; otherwise false</returns>
        [BindableMethod]
        public bool IsArchive(string extension) => IsArchiveSet.Contains(extension);

        /// <summary>
        /// Determines whether the file is archive.
        /// </summary>
        /// <param name="fileInfo">FileInfo that must be examined whether is archive or not</param>
        /// <returns><see langword="true" />if the specified extension is archive; otherwise, <see langword="false" /></returns>
        [BindableMethod]
        public bool IsArchive([InjectSource] ExtendedFileInfo fileInfo) => IsArchiveSet.Contains(fileInfo.Extension);

        /// <summary>
        /// Determine whether the extension is audio.
        /// </summary>
        /// <param name="extension">The extension</param>
        /// <returns>True if specified extension is audio; otherwise false</returns>
        [BindableMethod]
        public bool IsAudio(string extension) => IsAudioSet.Contains(extension);
        
        /// <summary>
        /// Determine whether the extension is audio.
        /// </summary>
        /// <param name="fileInfo">The fileInfo</param>
        /// <returns>True if audio; otherwise false</returns>
        [BindableMethod]
        public bool IsAudio([InjectSource] ExtendedFileInfo fileInfo) => IsAudioSet.Contains(fileInfo.Extension);

        /// <summary>
        /// Determine whether the extension is book.
        /// </summary>
        /// <param name="extension">The extension</param>
        /// <returns>True if specified extension is book; otherwise false</returns>
        [BindableMethod]
        public bool IsBook(string extension) => IsBookSet.Contains(extension);

        /// <summary>
        /// Determine whether the extension is book.
        /// </summary>
        /// <param name="fileInfo">The fileInfo</param>
        /// <returns>True if book; otherwise false</returns>
        [BindableMethod]
        public bool IsBook([InjectSource] ExtendedFileInfo fileInfo) => IsBookSet.Contains(fileInfo.Extension);

        /// <summary>
        /// Determine whether the extension is document.
        /// </summary>
        /// <param name="extension">The extension</param>
        /// <returns>True if specified extension is document; otherwise false</returns>
        [BindableMethod]
        public bool IsDoc(string extension) => IsDocSet.Contains(extension);

        /// <summary>
        /// Determine whether the extension is document.
        /// </summary>
        /// <param name="fileInfo">The fileInfo</param>
        /// <returns>True if document; otherwise false</returns>
        [BindableMethod]
        public bool IsDoc([InjectSource] ExtendedFileInfo fileInfo) => IsDocSet.Contains(fileInfo.Extension);
        
        /// <summary>
        /// Determine whether the extension is image.
        /// </summary>
        /// <param name="extension">The extension</param>
        /// <returns>True if specified extension is image; otherwise false</returns>
        [BindableMethod]
        public bool IsImage(string extension) => IsImageSet.Contains(extension);
        
        /// <summary>
        /// Determine whether the extension is image.
        /// </summary>
        /// <param name="fileInfo">The fileInfo</param>
        /// <returns>True if image; otherwise false</returns>
        [BindableMethod]
        public bool IsImage([InjectSource] ExtendedFileInfo fileInfo) => IsImageSet.Contains(fileInfo.Extension);

        /// <summary>
        /// Determine whether the extension is source.
        /// </summary>
        /// <param name="extension">The extension</param>
        /// <returns>True if specified extension is source; otherwise false</returns>
        [BindableMethod]
        public bool IsSource(string extension) => IsSourceSet.Contains(extension);
        
        /// <summary>
        /// Determine whether the extension is source.
        /// </summary>
        /// <param name="fileInfo">The fileInfo</param>
        /// <returns>True if source; otherwise false</returns>
        [BindableMethod]
        public bool IsSource([InjectSource] ExtendedFileInfo fileInfo) => IsSourceSet.Contains(fileInfo.Extension);

        /// <summary>
        /// Determine whether the extension is video.
        /// </summary>
        /// <param name="extension">The extension</param>
        /// <returns>True if specified extension is video; otherwise false</returns>
        [BindableMethod]
        public bool IsVideo(string extension) => IsVideoSet.Contains(extension);

        /// <summary>
        /// Determine whether the extension is video.
        /// </summary>
        /// <param name="fileInfo">The fileInfo</param>
        /// <returns>True if video; otherwise false</returns>
        [BindableMethod]
        public bool IsVideo([InjectSource] ExtendedFileInfo fileInfo) => IsVideoSet.Contains(fileInfo.Extension);

        /// <summary>
        /// Gets the file content
        /// </summary>
        /// <param name="extendedFileInfo">The extendedFileInfo</param>
        /// <returns>String content of a file</returns>
        [BindableMethod]
        public string GetFileContent([InjectSource] ExtendedFileInfo extendedFileInfo)
        {
            if (!extendedFileInfo.Exists)
                return null;

            using var file = extendedFileInfo.OpenRead();
            using var fileReader = new StreamReader(file);
            return fileReader.ReadToEnd();
        }

        /// <summary>
        /// Gets the relative path of a file
        /// </summary>
        /// <param name="fileInfo">The fileInfo</param>
        /// <returns>Relative file path to ComputationRootDirectoryPath</returns>
        [BindableMethod]
        public string GetRelativePath([InjectSource] ExtendedFileInfo fileInfo)
        {
            if (fileInfo == null)
                return null;

            return fileInfo.FullName.Replace(fileInfo.ComputationRootDirectoryPath, string.Empty);
        }

        /// <summary>
        /// Gets the relative path of a file
        /// </summary>
        /// <param name="fileInfo">The fileInfo</param>
        /// <param name="basePath">The basePath</param>
        /// <returns>Relative file path to basePath</returns>
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

        /// <summary>
        /// Gets head bytes of a file
        /// </summary>
        /// <param name="file">The file</param>
        /// <param name="length">The length</param>
        /// <returns>Head bytes of a file</returns>
        [BindableMethod]
        public byte[] Head([InjectSource] ExtendedFileInfo file, int length)
            => GetFileBytes(file, length, 0);

        /// <summary>
        /// Gets tail bytes of a file
        /// </summary>
        /// <param name="file">The file</param>
        /// <param name="length">The length</param>
        /// <returns>Tail bytes of a file</returns>
        [BindableMethod]
        public byte[] Tail([InjectSource] ExtendedFileInfo file, int length)
        {
            if (file == null)
                throw new InjectSourceNullReferenceException(typeof(FileInfo));

            using var stream = file.OpenRead();
            using var reader = new BinaryReader(stream);
            var toRead = length < stream.Length ? length : stream.Length;

            var bytes = new byte[toRead];

            stream.Position = stream.Length - length;
            for (var i = 0; i < toRead; ++i)
                bytes[i] = reader.ReadByte();

            return bytes;
        }

        /// <summary>
        /// Gets file bytes of a file
        /// </summary>
        /// <param name="file">The file</param>
        /// <param name="bytesCount">The bytesCount</param>
        /// <param name="offset">The offset</param>
        /// <returns>Bytes of a file</returns>
        [BindableMethod]
        public byte[] GetFileBytes([InjectSource] ExtendedFileInfo file, long bytesCount = long.MaxValue, long offset = 0)
        {
            if (file == null)
                throw new InjectSourceNullReferenceException(typeof(FileInfo));

            using var stream = file.OpenRead();
            using var reader = new BinaryReader(stream);
            if (offset > 0)
                stream.Seek(offset, SeekOrigin.Begin);

            var toRead = bytesCount < stream.Length ? bytesCount : stream.Length;

            var bytes = new byte[toRead];

            for (var i = 0; i < toRead; ++i)
                bytes[i] = reader.ReadByte();

            return bytes;
        }

        /// <summary>
        /// Computes Sha1 hash of a file
        /// </summary>
        /// <param name="file">The file</param>
        /// <returns>Sha1 of a file</returns>
        [BindableMethod]
        public string Sha1File([InjectSource] ExtendedFileInfo file)
        {
            if (file == null)
                throw new InjectSourceNullReferenceException(typeof(ExtendedFileInfo));

            using var stream = file.OpenRead();
            return HashHelper.ComputeHash<SHA1CryptoServiceProvider>(stream);
        }

        /// <summary>
        /// Computes Sha256 hash of a file
        /// </summary>
        /// <param name="file">The file</param>
        /// <returns>Sha256 of a file</returns>
        [BindableMethod]
        public string Sha256File([InjectSource] ExtendedFileInfo file)
        {
            if (file == null)
                throw new InjectSourceNullReferenceException(typeof(ExtendedFileInfo));

            return Sha256File(file.FileInfo);
        }
        
        /// <summary>
        /// Computes Sha256 hash of a file
        /// </summary>
        /// <param name="file">The file</param>
        /// <returns>Sha1 of a file</returns>
        public string Sha256File(FileInfo file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            using var stream = file.OpenRead();
            return HashHelper.ComputeHash<SHA256CryptoServiceProvider>(stream);
        }

        /// <summary>
        /// Computes Md5 hash of a file
        /// </summary>
        /// <param name="file">The file</param>
        /// <returns>Md5 of a file</returns>
        [BindableMethod]
        public string Md5File([InjectSource] ExtendedFileInfo file)
        {
            if (file == null)
                throw new InjectSourceNullReferenceException(typeof(ExtendedFileInfo));

            using var stream = file.OpenRead();
            return HashHelper.ComputeHash<MD5CryptoServiceProvider>(stream);
        }

        /// <summary>
        /// Determine whether file has specific content
        /// </summary>
        /// <param name="file">The file</param>
        /// <param name="pattern">The pattern</param>
        /// <returns>True if has content; otherwise false</returns>
        /// <exception cref="InjectSourceNullReferenceException"></exception>
        [BindableMethod]
        public bool HasContent([InjectSource] ExtendedFileInfo file, string pattern)
        {
            if (file == null)
                throw new InjectSourceNullReferenceException(typeof(ExtendedFileInfo));

            using var stream = new StreamReader(file.OpenRead());
            var content = stream.ReadToEnd();
            return Regex.IsMatch(content, pattern);
        }

        /// <summary>
        /// Determine whether file has attribute
        /// </summary>
        /// <param name="file">The file</param>
        /// <param name="flags">The flags</param>
        /// <returns>True if has attribute; otherwise false</returns>
        [BindableMethod]
        public bool HasAttribute([InjectSource] ExtendedFileInfo file, long flags)
        {
            return (flags & Convert.ToUInt32(file.Attributes)) == flags;
        }

        /// <summary>
        /// Gets lines containing word
        /// </summary>
        /// <param name="file">The file</param>
        /// <param name="word">The word</param>
        /// <returns>Line containing searched word</returns>
        [BindableMethod]
        public string GetLinesContainingWord([InjectSource] ExtendedFileInfo file, string word)
        {
            if (file == null)
                throw new InjectSourceNullReferenceException(typeof(ExtendedFileInfo));

            using var stream = new StreamReader(file.OpenRead());
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

        /// <summary>
        /// Gets the file length
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="unit">The unit</param>
        /// <returns>File length</returns>
        [BindableMethod]
        public long GetFileLength([InjectSource] ExtendedFileInfo context, string unit = "b")
            => GetLengthOfFile(context, unit);

        /// <summary>
        /// Gets the file length
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="unit">The unit</param>
        /// <returns>File length</returns>
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

        /// <summary>
        /// Gets the SubPath from the path
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="nesting">The nesting</param>
        /// <returns>SubPath based on nesting</returns>
        [BindableMethod]
        public string SubPath([InjectSource] DirectoryInfo context, int nesting)
            => SubPath(context.FullName, nesting);

        /// <summary>
        /// Gets the SubPath from the path
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="nesting">The nesting</param>
        /// <returns>SubPath based on nesting</returns>
        [BindableMethod]
        public string SubPath([InjectSource] ExtendedFileInfo context, int nesting)
            => SubPath(context.Directory.FullName, nesting);

        /// <summary>
        /// Gets the relative SubPath from the path
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="nesting">The nesting</param>
        /// <returns>Relative subPath based on nesting</returns>
        [BindableMethod]
        public string RelativeSubPath([InjectSource] ExtendedFileInfo context, int nesting)
            => SubPath(GetRelativePath(context, context.ComputationRootDirectoryPath), nesting);

        /// <summary>
        /// Gets the relative SubPath from the path
        /// </summary>
        /// <param name="directoryPath">The directoryPath</param>
        /// <param name="nesting">The nesting</param>
        /// <returns>Relative subPath based on nesting</returns>
        [BindableMethod]
        public string SubPath(string directoryPath, int nesting)
        {
            if (directoryPath == null)
                return null;

            if (directoryPath == string.Empty)
                return null;

            if (nesting < 0)
                return string.Empty;

            var splitDirs = directoryPath.Split(Path.DirectorySeparatorChar);
            var subPathBuilder = new StringBuilder();

            subPathBuilder.Append(splitDirs[0]);

            if (nesting >= 1 && splitDirs.Length > 1)
            {
                subPathBuilder.Append(Path.DirectorySeparatorChar);

                for (int i = 1; i < nesting && i < splitDirs.Length - 1; ++i)
                {
                    subPathBuilder.Append(splitDirs[i]);
                    subPathBuilder.Append(Path.DirectorySeparatorChar);
                }

                subPathBuilder.Append(splitDirs[nesting < splitDirs.Length - 1 ? nesting : splitDirs.Length - 1]);
            }

            return subPathBuilder.ToString();
        }

        /// <summary>
        /// Gets the length of the file
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="unit">The unit</param>
        /// <returns>Length of a file</returns>
        [BindableMethod]
        public long Length([InjectSource] ExtendedFileInfo context, string unit = "b")
            => GetLengthOfFile(context, unit);

        /// <summary>
        /// Gets the file info
        /// </summary>
        /// <param name="fullPath">The fullPath</param>
        /// <returns>ExtendedFileInfo</returns>
        [BindableMethod]
        public ExtendedFileInfo GetFileInfo(string fullPath)
        {
            var fileInfo = new FileInfo(fullPath);
            return new ExtendedFileInfo(fileInfo, fileInfo.DirectoryName);
        }

        /// <summary>
        /// Gets extended file info
        /// </summary>
        /// <param name="context">The context</param>
        /// <returns>ExtendedFileInfo</returns>
        [BindableMethod]
        public ExtendedFileInfo GetExtendedFileInfo([InjectSource] ExtendedFileInfo context)
            => context;

        /// <summary>
        /// Gets zip entry file info
        /// </summary>
        /// <param name="zipArchiveEntry">The zipArchiveEntry</param>
        /// <returns>ExtendedFileInfo</returns>
        [BindableMethod]
        public ExtendedFileInfo GetZipEntryFileInfo([InjectSource] ZipArchiveEntry zipArchiveEntry)
        {
            var fileInfo = SchemaZipHelper.UnpackZipEntry(zipArchiveEntry, zipArchiveEntry.FullName, Path.GetTempPath());
            return new ExtendedFileInfo(fileInfo, fileInfo.DirectoryName);
        }

        /// <summary>
        /// Gets the count of lines of a file
        /// </summary>
        /// <param name="context">The context</param>
        /// <returns>ExtendedFileInfo</returns>
        [BindableMethod]
        public long CountOfLines([InjectSource] ExtendedFileInfo context)
        {
            if (context == null)
                throw new InjectSourceNullReferenceException(typeof(ExtendedFileInfo));

            using var stream = new StreamReader(context.OpenRead());
            var lines = 0;
            while (!stream.EndOfStream)
            {
                lines += 1;
                stream.ReadLine();
            }

            return lines;
        }

        /// <summary>
        /// Gets the count of non empty lines of a file
        /// </summary>
        /// <param name="context">The context</param>
        /// <returns>Count of non empty lines</returns>
        [BindableMethod]
        public long CountOfNotEmptyLines([InjectSource] ExtendedFileInfo context)
        {
            if (context == null)
                throw new InjectSourceNullReferenceException(typeof(ExtendedFileInfo));

            using var stream = new StreamReader(context.OpenRead());
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

        /// <summary>
        /// Compresses the directories and write to path
        /// </summary>
        /// <param name="directories">The directories</param>
        /// <param name="path">The path</param>
        /// <param name="method">The method</param>
        /// <returns>Path to compressed directories</returns>
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

            var operationExecutedSuccessfully = true;
            using (var zipArchiveFile = File.Open(path, FileMode.OpenOrCreate))
            {
                try
                {
                    using var zip = new ZipArchive(zipArchiveFile, ZipArchiveMode.Create);
                    var dirs = new Stack<DirectoryInfoPosition>();

                    foreach (var dir in directories)
                        dirs.Push(new DirectoryInfoPosition(dir, dir.Parent));

                    while (dirs.Count > 0)
                    {
                        var dir = dirs.Pop();

                        foreach (var file in dir.Directory.GetFiles())
                        {
                            var entryName = file.FullName.Substring(dir.RootDirectory.FullName.Length);
                            zip.CreateEntryFromFile(file.FullName, entryName.Trim('\\'), level);
                        }

                        foreach (var subDir in dir.Directory.GetDirectories())
                            dirs.Push(new DirectoryInfoPosition(subDir, dir.RootDirectory));
                    }
                }
                catch (Exception)
                {
                    operationExecutedSuccessfully = false;
                }
            }

            return operationExecutedSuccessfully ? path : string.Empty;
        }

        /// <summary>
        /// Compresses the files and write to path
        /// </summary>
        /// <param name="files">The directories</param>
        /// <param name="path">The path</param>
        /// <param name="method">The method</param>
        /// <returns>Path to compressed directories</returns>
        [BindableMethod]
        public string Compress(IReadOnlyList<ExtendedFileInfo> files, string path, string method)
        {
            if (files.Count == 0)
                return string.Empty;

            var level = method.ToLowerInvariant() switch
            {
                "fastest" => CompressionLevel.Fastest,
                "optimal" => CompressionLevel.Optimal,
                "nocompression" => CompressionLevel.NoCompression,
                _ => throw new NotSupportedException(method)
            };

            var operationExecutedSuccessfully = true;
            using (var zipArchiveFile = File.Open(path, FileMode.OpenOrCreate))
            {
                try
                {
                    using var zip = new ZipArchive(zipArchiveFile, ZipArchiveMode.Create);
                    foreach (var file in files) zip.CreateEntryFromFile(file.FullName, file.Name, level);
                }
                catch (Exception)
                {
                    operationExecutedSuccessfully = false;
                }
            }

            return operationExecutedSuccessfully ? path : string.Empty;
        }

        /// <summary>
        /// Decompresses the files and write to path
        /// </summary>
        /// <param name="files">The directories</param>
        /// <param name="path">The path</param>
        /// <returns>Path to decompressed files</returns>
        [BindableMethod]
        public string Decompress(IReadOnlyList<ExtendedFileInfo> files, string path)
        {
            if (files.Count == 0)
                return string.Empty;

            var operationExecutedSuccessfully = true;

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

                operationExecutedSuccessfully = false;
            }

            return operationExecutedSuccessfully ? path : string.Empty;
        }

        /// <summary>
        /// Combines the paths
        /// </summary>
        /// <param name="path1">The path1</param>
        /// <param name="path2">The path2</param>
        /// <returns>Combined paths</returns>
        [BindableMethod]
        public string Combine(string path1, string path2)
        {
            return Path.Combine(path1, path2);
        }
        
        /// <summary>
        /// Combines the paths
        /// </summary>
        /// <param name="path1">The path1</param>
        /// <param name="path2">The path2</param>
        /// <param name="path3">The path3</param>
        /// <returns>Combined paths</returns>
        [BindableMethod]
        public string Combine(string path1, string path2, string path3)
        {
            return Path.Combine(path1, path2, path3);
        }
        
        /// <summary>
        /// Combines the paths
        /// </summary>
        /// <param name="path1">The path1</param>
        /// <param name="path2">The path2</param>
        /// <param name="path3">The path3</param>
        /// <param name="path4">The path4</param>
        /// <returns>Combined paths</returns>
        [BindableMethod]
        public string Combine(string path1, string path2, string path3, string path4)
        {
            return Path.Combine(path1, path2, path3, path4);
        }
        
        /// <summary>
        /// Combines the paths
        /// </summary>
        /// <param name="path1">The path1</param>
        /// <param name="path2">The path2</param>
        /// <param name="path3">The path3</param>
        /// <param name="path4">The path4</param>
        /// <param name="path5">The path5</param>
        /// <returns>Combined paths</returns>
        [BindableMethod]
        public string Combine(string path1, string path2, string path3, string path4, string path5)
        {
            return Path.Combine(path1, path2, path3, path4, path5);
        }
        
        /// <summary>
        /// Combines the paths
        /// </summary>
        /// <param name="paths">The paths</param>
        /// <returns>Combined paths</returns>
        [BindableMethod]
        public string Combine(params string[] paths)
        {
            return Path.Combine(paths);
        }

        /// <summary>
        /// Gets the zip archive entry
        /// </summary>
        /// <param name="zipArchiveEntry">The zipArchiveEntry</param>
        /// <returns>ZipArchiveEntry</returns>
        [BindableMethod]
        public ZipArchiveEntry GetZipArchiveEntry([InjectSource] ZipArchiveEntry zipArchiveEntry)
        {
            return zipArchiveEntry;
        }

        /// <summary>
        /// Unpacks to destination directory
        /// </summary>
        /// <param name="zipArchiveEntry">The zipArchiveEntry</param>
        /// <param name="destinationDirectory">The destinationDirectory</param>
        /// <returns>ZipArchiveEntry</returns>
        [BindableMethod]
        public string UnpackTo([InjectSource] ZipArchiveEntry zipArchiveEntry, string destinationDirectory)
        {
            var fileInfo = SchemaZipHelper.UnpackZipEntry(zipArchiveEntry, zipArchiveEntry.FullName, destinationDirectory);
            
            return fileInfo.FullName;
        }

        /// <summary>
        /// Unpacks to temp directory
        /// </summary>
        /// <param name="zipArchiveEntry">The zipArchiveEntry</param>
        /// <returns>Path to unpacked file</returns>
        [BindableMethod]
        public string Unpack([InjectSource] ZipArchiveEntry zipArchiveEntry)
        {
            return UnpackTo(zipArchiveEntry, Path.GetTempPath());
        }

        private class DirectoryInfoPosition
        {
            public DirectoryInfoPosition(DirectoryInfo dir, DirectoryInfo root)
            {
                Directory = dir;
                RootDirectory = root;
            }

            public DirectoryInfo Directory { get; }

            public DirectoryInfo RootDirectory { get; }
        }
    }
}