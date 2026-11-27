using System.Collections;
using System.Collections.Generic;

namespace Musoq.Schema.DataSources;

/// <summary>Opts a record-valued APPLY source into processing each row as its enumerator yields it.</summary>
public interface IStreamingApplyEnumerable<out T> : IEnumerable<T> { }

public static class ApplyRows
{
    /// <summary>Allows a query to consume a row before requesting the next one, and to stop enumeration early.</summary>
    public static IStreamingApplyEnumerable<T> Stream<T>(IEnumerable<T> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return rows as IStreamingApplyEnumerable<T> ?? new StreamingRows<T>(rows);
    }

    private sealed class StreamingRows<T>(IEnumerable<T> rows) : IStreamingApplyEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator() => rows.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
