using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
    private readonly Lazy<MetadataReference[]> _references;

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
        _references = new Lazy<MetadataReference[]>(
            LoadReferences,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public MetadataReference[] References
    {
        get => _references.Value.ToArray();
    }

    public void CreateReferences()
    {
        _ = _references.Value;
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
