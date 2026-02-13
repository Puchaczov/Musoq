using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;

namespace Musoq.Converter.Cache;

/// <summary>
///     Thread-safe cache for compiled query assemblies (DLL/PDB bytes).
///     Avoids repeated Roslyn Compilation.Emit() calls for queries that produce
///     semantically identical generated C# code.
/// </summary>
public static class CompiledQueryCache
{
    private static readonly Regex NamespaceIdPattern =
        new(@"Query\.Compiled_\d+", RegexOptions.Compiled);

    private static readonly ConcurrentDictionary<string, CachedEmitResult> Cache = new();

    /// <summary>
    ///     Gets the current number of cached compilations.
    /// </summary>
    public static int Count => Cache.Count;

    /// <summary>
    ///     Attempts to retrieve a cached emit result for the given compilation.
    ///     The cache key is a SHA256 hash of the normalized syntax tree texts.
    /// </summary>
    /// <param name="compilation">The CSharp compilation whose syntax trees define the cache key.</param>
    /// <param name="result">The cached result if found; null otherwise.</param>
    /// <returns>True if a cache hit was found.</returns>
    public static bool TryGet(CSharpCompilation compilation, out CachedEmitResult result)
    {
        var hash = ComputeHash(compilation);
        return Cache.TryGetValue(hash, out result);
    }

    /// <summary>
    ///     Stores an emit result in the cache, keyed by the compilation's normalized syntax tree hash.
    /// </summary>
    /// <param name="compilation">The CSharp compilation whose syntax trees define the cache key.</param>
    /// <param name="dllFile">The emitted DLL bytes.</param>
    /// <param name="pdbFile">The emitted PDB bytes.</param>
    /// <param name="accessToClassPath">
    ///     The fully qualified type name for the compiled query
    ///     (e.g., "Query.Compiled_42.CompiledQuery"), which is baked into the DLL.
    /// </param>
    public static void Store(CSharpCompilation compilation, byte[] dllFile, byte[] pdbFile,
        string accessToClassPath)
    {
        var hash = ComputeHash(compilation);
        Cache.TryAdd(hash, new CachedEmitResult(dllFile, pdbFile, accessToClassPath));
    }

    /// <summary>
    ///     Clears all cached compilations.
    /// </summary>
    public static void Clear()
    {
        Cache.Clear();
    }

    /// <summary>
    ///     Computes a SHA256 hash of all syntax trees in the compilation,
    ///     with namespace identifiers normalized so that different compilations
    ///     of the same query produce the same hash.
    /// </summary>
    private static string ComputeHash(CSharpCompilation compilation)
    {
        var normalizedText = GetNormalizedText(compilation);
        var bytes = Encoding.UTF8.GetBytes(normalizedText);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    ///     Returns the normalized text of all syntax trees in the compilation for diagnostic purposes.
    /// </summary>
    public static string GetNormalizedText(CSharpCompilation compilation)
    {
        var syntaxTrees = compilation.SyntaxTrees
            .OrderBy(t => t.FilePath, StringComparer.Ordinal)
            .ToList();

        var sb = new StringBuilder();

        foreach (var tree in syntaxTrees)
        {
            var text = tree.ToString();
            var normalized = NamespaceIdPattern.Replace(text, "Query.Compiled_NORMALIZED");
            sb.Append(normalized);
            sb.Append('\0'); // separator between trees
        }

        return sb.ToString();
    }
}
