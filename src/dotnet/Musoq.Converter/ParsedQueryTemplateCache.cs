using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;

namespace Musoq.Converter;

internal static class ParsedQueryTemplateCache
{
    internal const string DefaultParserContract = "compose-all/skip-whitespace/v1";

    private const int MaximumEntries = 256;
    private const int MaximumRetainedTextCharacters = 4_000_000;
    private static readonly ConcurrentDictionary<CacheKey, Lazy<CacheEntry>> Entries = new();
    private static readonly object EvictionGate = new();
    private static readonly Queue<QueueItem> InsertionOrder = new();
    private static long _hits;
    private static long _misses;
    private static int _retainedTextCharacters;

    internal static ParsedQueryTemplateCacheSnapshot Snapshot
    {
        get
        {
            lock (EvictionGate)
            {
                return new ParsedQueryTemplateCacheSnapshot(
                    Entries.Count,
                    _retainedTextCharacters,
                    Interlocked.Read(ref _hits),
                    Interlocked.Read(ref _misses));
            }
        }
    }

    internal static RootNode GetOrAdd(
        string script,
        string parserContract,
        Func<RootNode> factory)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(parserContract);
        ArgumentNullException.ThrowIfNull(factory);

        if (script.Length > MaximumRetainedTextCharacters)
            return factory();

        var key = new CacheKey(script, parserContract);
        if (Entries.TryGetValue(key, out var existing))
        {
            Interlocked.Increment(ref _hits);
            return CloneForCompilation(existing.Value.Root);
        }

        var candidate = new Lazy<CacheEntry>(
            () => new CacheEntry(factory()),
            LazyThreadSafetyMode.ExecutionAndPublication);
        var winner = Entries.GetOrAdd(key, candidate);
        if (!ReferenceEquals(candidate, winner))
        {
            Interlocked.Increment(ref _hits);
            return CloneForCompilation(winner.Value.Root);
        }

        Interlocked.Increment(ref _misses);
        try
        {
            _ = winner.Value;
            Publish(key, winner);
            return CloneForCompilation(winner.Value.Root);
        }
        catch
        {
            Remove(key, winner);
            throw;
        }
    }

    internal static RootNode GetOrAdd(string script, Func<RootNode> factory)
    {
        return GetOrAdd(script, DefaultParserContract, factory);
    }

    internal static void Clear()
    {
        lock (EvictionGate)
        {
            Entries.Clear();
            InsertionOrder.Clear();
            _retainedTextCharacters = 0;
            Interlocked.Exchange(ref _hits, 0);
            Interlocked.Exchange(ref _misses, 0);
        }
    }

    private static void Publish(CacheKey key, Lazy<CacheEntry> value)
    {
        lock (EvictionGate)
        {
            if (!Entries.TryGetValue(key, out var current) || !ReferenceEquals(current, value))
                return;

            InsertionOrder.Enqueue(new QueueItem(key, value));
            _retainedTextCharacters += key.Script.Length;
            TrimToBounds();
        }
    }

    private static void TrimToBounds()
    {
        while (Entries.Count > MaximumEntries || _retainedTextCharacters > MaximumRetainedTextCharacters)
        {
            if (InsertionOrder.Count == 0)
                return;

            var item = InsertionOrder.Dequeue();
            if (!Entries.TryGetValue(item.Key, out var current) || !ReferenceEquals(current, item.Value))
                continue;

            RemoveEntry(item.Key, item.Value);
        }
    }

    private static void Remove(CacheKey key, Lazy<CacheEntry> value)
    {
        lock (EvictionGate)
            RemoveEntry(key, value);
    }

    private static void RemoveEntry(CacheKey key, Lazy<CacheEntry> value)
    {
        if (!((ICollection<KeyValuePair<CacheKey, Lazy<CacheEntry>>>)Entries)
                .Remove(new KeyValuePair<CacheKey, Lazy<CacheEntry>>(key, value)))
            return;

        _retainedTextCharacters = Math.Max(0, _retainedTextCharacters - key.Script.Length);
    }

    internal static RootNode CloneForCompilation(RootNode root)
    {
        var visitor = new CloneQueryVisitor();
        root.Accept(new CloneTraverseVisitor(visitor));
        return visitor.Root;
    }

    private readonly record struct CacheKey(string Script, string ParserContract);

    private readonly record struct QueueItem(CacheKey Key, Lazy<CacheEntry> Value);

    private sealed record CacheEntry(RootNode Root);
}

internal readonly record struct ParsedQueryTemplateCacheSnapshot(
    int Count,
    int RetainedTextCharacters,
    long Hits,
    long Misses);
