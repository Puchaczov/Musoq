using System;
using System.IO;

namespace Musoq.Schema.Os.Files
{
    public class ExtendedFileInfo
    {
        public ExtendedFileInfo(FileInfo fileInfo, string computationRootDirectoryPath)
        {
            FileInfo = fileInfo;
            ComputationRootDirectoryPath = computationRootDirectoryPath;
        }

        public string ComputationRootDirectoryPath { get; }

        public FileInfo FileInfo { get; }

        public bool IsReadOnly => FileInfo.IsReadOnly;
        
        public bool Exists => FileInfo.Exists;
        
        public string DirectoryName => FileInfo.DirectoryName;
        
        public DirectoryInfo Directory => FileInfo.Directory;
        
        public long Length => FileInfo.Length;
        
        public string Name => FileInfo.Name;

        public DateTime CreationTime => FileInfo.CreationTime;

        public DateTime CreationTimeUtc => FileInfo.CreationTimeUtc;

        public string Extension => FileInfo.Extension;

        public string FullName => FileInfo.FullName;

        public FileStream OpenRead() => FileInfo.OpenRead();

        public FileAttributes Attributes => FileInfo.Attributes;
    }
}