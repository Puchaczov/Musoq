using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Musoq.Evaluator.Runtime;

internal sealed class DefaultMetadataReferenceCache : IMetadataReferenceCache
{
    private readonly ConcurrentDictionary<string, Lazy<MetadataReference>> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public int Count => _cache.Count;

    public MetadataReference GetOrCreate(string assemblyPath)
    {
        if (string.IsNullOrEmpty(assemblyPath))
            throw new ArgumentNullException(nameof(assemblyPath));

        var lazyReference = _cache.GetOrAdd(
            assemblyPath,
            static path => new Lazy<MetadataReference>(
                () => MetadataReference.CreateFromFile(path),
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return lazyReference.Value;
        }
        catch
        {
            _cache.TryRemove(assemblyPath, out _);
            throw;
        }
    }

    public void Clear()
    {
        _cache.Clear();
    }
}
