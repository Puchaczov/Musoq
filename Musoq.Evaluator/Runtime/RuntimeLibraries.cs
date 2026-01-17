using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Musoq.Evaluator.Runtime;

public static class RuntimeLibraries
{
    private static MetadataReference[] _references;
    private static bool _hasLoadedReferences;
    private static readonly object LockGuard = new();
    private static Task _loadingTask;
    private static bool _readInProgress;
    private static bool _readFinished;
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

        if (_readFinished)
            return;

        lock (LockGuard)
        {
            if (_readInProgress)
                return;

            _readInProgress = true;

            _loadingTask = Task.Factory.StartNew(() =>
            {
                try
                {
                    var objLocation = typeof(object).GetTypeInfo().Assembly.Location;
                    var path = new FileInfo(objLocation);
                    var directory = path.Directory;

                    if (directory is null) return;

                    var essentialAssemblies = new[]
                    {
                        "System.Private.CoreLib.dll",
                        "System.Runtime.dll",
                        "System.Collections.dll",
                        "System.Collections.Concurrent.dll",
                        "System.Linq.dll",
                        "System.Threading.Tasks.dll",
                        "System.Threading.Tasks.Parallel.dll",
                        "System.Linq.Expressions.dll",
                        "Microsoft.CSharp.dll"
                    };

                    var files = essentialAssemblies
                        .Select(name => new FileInfo(Path.Combine(directory.FullName, name)))
                        .Where(fi => fi.Exists)
                        .ToList();

                    var references = new List<MetadataReference>(files.Count);

                    foreach (var file in files)
                    {
                        if (file.Name.Contains("native", StringComparison.InvariantCultureIgnoreCase))
                            continue;

                        try
                        {
                            references.Add(MetadataReferenceCache.GetOrCreate(file.FullName));
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
                    }

                    _references = references.ToArray();
                }
                finally
                {
                    _hasLoadedReferences = true;
                    // ReSharper disable once InconsistentlySynchronizedField
                    _readInProgress = false;
                    _readFinished = true;

                    ManualResetEvent.Set();
                }
            });
        }
    }
}