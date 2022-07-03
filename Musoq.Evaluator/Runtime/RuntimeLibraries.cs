using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Musoq.Evaluator.Runtime
{
    public static class RuntimeLibraries
    {
        private static MetadataReference[] _references;
        private static bool _hasLoadedReferences;
        private static readonly object LockGuard = new();
        private static Task _loadingTask;
        private static bool _readInProgress;
        private static readonly ManualResetEvent ManualResetEvent = new(false);

        public static MetadataReference[] References
        {
            get
            {
                if (_hasLoadedReferences)
                    return _references;


                CreateReferences();
                ManualResetEvent.WaitOne();

                return _references;
            }
        }

        public static void CreateReferences()
        {
            if (_hasLoadedReferences)
                return;

            if (_readInProgress)
                return;

            lock (LockGuard)
            {
                if(_readInProgress)
                    return;

                _readInProgress = true;

                _loadingTask = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var objLocation = typeof(object).GetTypeInfo().Assembly.Location;
                        var path = new FileInfo(objLocation);
                        var directory = path.Directory;

                        var tasks = new List<Task<MetadataReference>>();

                        var directories = directory.GetFiles("System*.dll").ToList();

                        var microsoftCSharpFileInfo = new FileInfo(Path.Combine(directory.FullName, "Microsoft.CSharp.dll"));

                        if (microsoftCSharpFileInfo.Exists)
                            directories.Add(microsoftCSharpFileInfo);

                        foreach (var file in directories)
                        {
                            if (file.Name.ToLowerInvariant().Contains("native"))
                            {
                                continue;
                            }
                            
                            tasks.Add(Task.Factory.StartNew<MetadataReference>(() =>
                            {
                                try
                                {
                                    return MetadataReference.CreateFromFile(file.FullName);
                                }
                                catch (FileNotFoundException)
                                {
                                }
                                catch (BadImageFormatException)
                                {
                                }
                                catch (FileLoadException)
                                {
                                }

                                return null;
                            }));
                        }

                        Task.WaitAll(tasks.ToArray());

                        _references = tasks.Where(task => task.Result != null).Select(task => task.Result).ToArray();
                    }
                    finally
                    {
                        _hasLoadedReferences = true;

                        ManualResetEvent.Set();
                    }
                });
            }
        }
    }
}

