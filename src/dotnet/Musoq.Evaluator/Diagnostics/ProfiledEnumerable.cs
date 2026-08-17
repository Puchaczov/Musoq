using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Diagnostics;

public sealed class ProfiledEnumerable<T> : IEnumerable<T>
{
    private readonly IEnumerable<T> _source;
    private readonly SourceProfileRecorder _recorder;

    public ProfiledEnumerable(IEnumerable<T> source, SourceProfileRecorder recorder)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(recorder);

        _source = source;
        _recorder = recorder;
    }

    public static IEnumerable<T> Create(IEnumerable<T> source, SourceProfileRecorder recorder)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(recorder);

        if (source.TryGetNonEnumeratedCount(out var count))
            return new CountPreservingProfiledEnumerable<T>(source, recorder, count);

        return new ProfiledEnumerable<T>(source, recorder);
    }

    public IEnumerator<T> GetEnumerator() =>
        new ProfiledEnumerator<T>(CreateSourceEnumerator(_source, _recorder), _recorder);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class CountPreservingProfiledEnumerable<TItem>(
        IEnumerable<TItem> source,
        SourceProfileRecorder recorder,
        int count) : ICollection<TItem>, IReadOnlyCollection<TItem>
    {
        public int Count { get; } = count;

        public bool IsReadOnly => true;

        public IEnumerator<TItem> GetEnumerator() =>
            new ProfiledEnumerator<TItem>(CreateSourceEnumerator(source, recorder), recorder);

        public bool Contains(TItem item)
        {
            var comparer = EqualityComparer<TItem>.Default;
            foreach (var candidate in this)
            {
                if (comparer.Equals(candidate, item))
                    return true;
            }

            return false;
        }

        public void CopyTo(TItem[] array, int arrayIndex)
        {
            foreach (var item in this)
                array[arrayIndex++] = item;
        }

        public void Add(TItem item) => ThrowReadOnlyCollectionMutation();

        public bool Remove(TItem item)
        {
            ThrowReadOnlyCollectionMutation();
            return false;
        }

        public void Clear() => ThrowReadOnlyCollectionMutation();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static void ThrowReadOnlyCollectionMutation() =>
            throw new NotSupportedException("Profiled count-preserving wrappers are read-only.");
    }

    private static IEnumerator<TItem> CreateSourceEnumerator<TItem>(
        IEnumerable<TItem> source,
        SourceProfileRecorder recorder)
    {
        var clock = recorder.Clock;
        var startedTimestamp = clock.GetTimestamp();
        var operatorExclusionTarget = recorder.CaptureCurrentOperatorExclusionTarget();
        try
        {
            return source.GetEnumerator();
        }
        catch (Exception exception)
        {
            var endTimestamp = clock.GetTimestamp();
            var sourceWaitTime = clock.GetElapsedTime(startedTimestamp, endTimestamp);
            recorder.RecordEnumeration(new SourceEnumerationProfileBatch(
                true,
                startedTimestamp,
                0,
                false,
                0,
                0,
                sourceWaitTime,
                TimeSpan.Zero,
                1,
                exception.GetType().FullName,
                exception.Message,
                false,
                1,
                0));
            operatorExclusionTarget.ExcludeElapsedTicks(sourceWaitTime.Ticks);
            throw;
        }
    }
}
