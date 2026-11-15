using System;
using System.Collections.Concurrent;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests.Schema.LibraryExecution;

public sealed class ArrayIndexEvaluationFixture : IDisposable
{
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<string>> Traces = new();
    private readonly ConcurrentQueue<string> _trace = new();

    public ArrayIndexEvaluationFixture() => Traces[Key] = _trace;

    public string Key { get; } = Guid.NewGuid().ToString("N");
    public string[] Evaluations => _trace.ToArray();

    public void Dispose() => Traces.TryRemove(Key, out _);

    public sealed record Input(int[]? Items, int Position)
    {
        public Entry[] Records { get; } = [new(10, [100, 200]), new(20, [300, 400])];
        public int?[] OptionalItems { get; } = [10, null];
    }

    public sealed record Entry(int Value, int[] Values);

    public sealed class Functions : LibraryBase
    {
        [BindableMethod]
        [NonDeterministic]
        public int[]? MarkItems(string key, int[]? items)
        {
            Traces[key].Enqueue("receiver");
            return items;
        }

        [BindableMethod]
        [NonDeterministic]
        public int MarkIndex(string key, int index)
        {
            Traces[key].Enqueue("index");
            return index;
        }
    }
}
