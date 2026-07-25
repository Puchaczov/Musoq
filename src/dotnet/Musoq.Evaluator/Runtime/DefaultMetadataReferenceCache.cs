using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;

namespace Musoq.Evaluator.Runtime;

internal sealed class DefaultMetadataReferenceCache : IMetadataReferenceCache
{
    private readonly BoundedRuntimeCache<MetadataReferenceCacheKey, MetadataReference> _cache;

    public DefaultMetadataReferenceCache(int maxSize = RuntimeCacheOptions.MetadataReferenceCacheSize)
    {
        _cache = new BoundedRuntimeCache<MetadataReferenceCacheKey, MetadataReference>(
            maxSize,
            MetadataReferenceCacheKeyComparer.Instance);
    }

    public int Count => _cache.Count;

    public MetadataReference GetOrCreate(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
            throw new ArgumentNullException(nameof(assemblyPath));

        var key = MetadataReferenceCacheKey.Create(assemblyPath);
        return _cache.GetOrAdd(key, static cacheKey => MetadataReference.CreateFromFile(cacheKey.Path));
    }

    public void Clear()
    {
        _cache.Clear();
    }

    private readonly record struct MetadataReferenceCacheKey(
        string Path,
        long Length,
        long LastWriteTimeUtcTicks,
        string ContentHash)
    {
        public static MetadataReferenceCacheKey Create(string path)
        {
            var fullPath = System.IO.Path.GetFullPath(path);
            var fileInfo = new FileInfo(fullPath);
            using var stream = File.OpenRead(fullPath);
            var contentHash = Convert.ToHexString(SHA256.HashData(stream));
            return new MetadataReferenceCacheKey(
                fullPath,
                fileInfo.Length,
                fileInfo.LastWriteTimeUtc.Ticks,
                contentHash);
        }
    }

    private sealed class MetadataReferenceCacheKeyComparer : IEqualityComparer<MetadataReferenceCacheKey>
    {
        public static MetadataReferenceCacheKeyComparer Instance { get; } = new();

        public bool Equals(MetadataReferenceCacheKey x, MetadataReferenceCacheKey y) =>
            x.Length == y.Length &&
            x.LastWriteTimeUtcTicks == y.LastWriteTimeUtcTicks &&
            StringComparer.Ordinal.Equals(x.ContentHash, y.ContentHash) &&
            StringComparer.OrdinalIgnoreCase.Equals(x.Path, y.Path);

        public int GetHashCode(MetadataReferenceCacheKey obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Path),
                obj.Length,
                obj.LastWriteTimeUtcTicks,
                StringComparer.Ordinal.GetHashCode(obj.ContentHash));
    }
}
