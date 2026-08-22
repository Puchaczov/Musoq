namespace Musoq.Evaluator;

/// <summary>
///     Controls the publication cadence for approximate query progress.
/// </summary>
public sealed class QueryProgressOptions
{
    public const long DefaultRowsPerUpdate = 16_384;

    public static readonly TimeSpan DefaultMinimumInterval = TimeSpan.FromMilliseconds(250);

    private long _rowsPerUpdate = DefaultRowsPerUpdate;
    private TimeSpan _minimumInterval = DefaultMinimumInterval;

    public long RowsPerUpdate
    {
        get => _rowsPerUpdate;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _rowsPerUpdate = value;
        }
    }

    public TimeSpan MinimumInterval
    {
        get => _minimumInterval;
        init
        {
            if (value < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value));

            _minimumInterval = value;
        }
    }

    /// <summary>
    ///     Supplies timestamps for deterministic cadence tests. Production callers normally use the default.
    /// </summary>
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;
}
