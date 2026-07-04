using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Musoq.Evaluator.Runtime;

internal sealed class RuntimeReferenceProvider : IRuntimeReferenceProvider
{
    private static readonly string[] DefaultEssentialAssemblyNames =
    [
        "System.Private.CoreLib.dll",
        "System.Runtime.dll",
        "System.Collections.dll",
        "System.Collections.Concurrent.dll",
        "System.Collections.Immutable.dll",
        "System.Linq.dll",
        "System.Threading.Tasks.dll",
        "System.Threading.Tasks.Parallel.dll",
        "System.Linq.Expressions.dll",
        "Microsoft.CSharp.dll",
        "System.Text.RegularExpressions.dll",
        "System.ObjectModel.dll",
        "System.Dynamic.Runtime.dll",
        "System.ComponentModel.Primitives.dll"
    ];

    private readonly IMetadataReferenceCache _referenceCache;
    private readonly Func<string?> _runtimeDirectoryProvider;
    private readonly IReadOnlyList<string> _essentialAssemblyNames;
    private readonly object _lockGuard = new();
    private readonly ManualResetEvent _manualResetEvent = new(false);
    private MetadataReference[]? _references;
    private bool _hasLoadedReferences;
    private bool _readInProgress;
    private bool _readFinished;

    public RuntimeReferenceProvider(IMetadataReferenceCache referenceCache)
        : this(referenceCache, GetDefaultRuntimeDirectory, DefaultEssentialAssemblyNames)
    {
    }

    internal RuntimeReferenceProvider(
        IMetadataReferenceCache referenceCache,
        Func<string?> runtimeDirectoryProvider,
        IEnumerable<string>? essentialAssemblyNames = null)
    {
        _referenceCache = referenceCache ?? throw new ArgumentNullException(nameof(referenceCache));
        _runtimeDirectoryProvider = runtimeDirectoryProvider ?? throw new ArgumentNullException(nameof(runtimeDirectoryProvider));
        _essentialAssemblyNames = (essentialAssemblyNames ?? DefaultEssentialAssemblyNames).ToArray();
    }

    public MetadataReference[] References
    {
        get
        {
            if (_hasLoadedReferences)
                return _references ?? [];

            CreateReferences();
            _manualResetEvent.WaitOne();

            return _references ?? [];
        }
    }

    public void CreateReferences()
    {
        if (_hasLoadedReferences)
            return;

        if (_readFinished)
            return;

        lock (_lockGuard)
        {
            if (_readInProgress)
                return;

            _readInProgress = true;

            _ = Task.Run(() =>
            {
                try
                {
                    _references = LoadReferences();
                }
                finally
                {
                    _hasLoadedReferences = true;
                    _readInProgress = false;
                    _readFinished = true;
                    _manualResetEvent.Set();
                }
            });
        }
    }

    private MetadataReference[] LoadReferences()
    {
        var runtimeDirectory = _runtimeDirectoryProvider();
        if (runtimeDirectory is null)
            return [];

        var references = new List<MetadataReference>(_essentialAssemblyNames.Count);

        foreach (var assemblyName in _essentialAssemblyNames)
        {
            var file = new FileInfo(Path.Combine(runtimeDirectory, assemblyName));
            if (!file.Exists)
                continue;

            if (file.Name.Contains("native", StringComparison.InvariantCultureIgnoreCase))
                continue;

            try
            {
                references.Add(_referenceCache.GetOrCreate(file.FullName));
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

        return references.ToArray();
    }

    private static string? GetDefaultRuntimeDirectory()
    {
        var objLocation = typeof(object).Assembly.Location;
        var path = new FileInfo(objLocation);

        return path.Directory?.FullName;
    }
}
