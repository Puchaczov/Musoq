using System;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Musoq.Evaluator.Runtime;

internal sealed class DefaultMetadataReferenceCache : IMetadataReferenceCache
{
    private readonly BoundedRuntimeCache<string, MetadataReferenceEntry> _cache;

    public DefaultMetadataReferenceCache(int maxSize = RuntimeCacheOptions.MetadataReferenceCacheSize)
    {
        _cache = new BoundedRuntimeCache<string, MetadataReferenceEntry>(
            maxSize,
            StringComparer.OrdinalIgnoreCase);
    }

    public int Count => _cache.Count;

    public MetadataReference GetOrCreate(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
            throw new ArgumentNullException(nameof(assemblyPath));

        var path = Path.GetFullPath(assemblyPath);
        var fileInfo = new FileInfo(path);
        var length = fileInfo.Length;
        var lastWriteTimeUtcTicks = fileInfo.LastWriteTimeUtc.Ticks;

        if (_cache.TryGetValue(path, out var cached) &&
            cached.Matches(length, lastWriteTimeUtcTicks))
            return cached.Reference;

        var entry = _cache.GetOrAdd(
            path,
            static cachePath =>
            {
                var info = new FileInfo(cachePath);
                return new MetadataReferenceEntry(
                    info.Length,
                    info.LastWriteTimeUtc.Ticks,
                    MetadataReference.CreateFromFile(cachePath));
            },
            candidate => candidate.Matches(length, lastWriteTimeUtcTicks));
        return entry.Reference;
    }

    public void Clear()
    {
        _cache.Clear();
    }

    private readonly record struct MetadataReferenceEntry(
        long Length,
        long LastWriteTimeUtcTicks,
        MetadataReference Reference)
    {
        public bool Matches(long length, long lastWriteTimeUtcTicks) =>
            Length == length && LastWriteTimeUtcTicks == lastWriteTimeUtcTicks;
    }
}
