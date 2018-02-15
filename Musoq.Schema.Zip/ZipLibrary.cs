using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Schema.Zip
{
    public class ZipLibrary : LibraryBase
    {
        [AggregationSetMethod]
        public void SetAggregateFiles([InjectGroup] Group group, string name, FileInfo file)
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
                    if(rootDirs.All(f => dir.Key.Contains('\\') || !f.FullName.Substring(tempPath.Length).EndsWith(dir.Key)))
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

                            if(File.Exists(destPath))
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