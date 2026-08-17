namespace Musoq.Evaluator.Diagnostics;

internal readonly record struct SourceEnumerationProfileBatch(
    bool HasStarted,
    long StartedTimestamp,
    long RowsRead,
    bool HasFirstRow,
    long FirstRowTimestamp,
    long LastRowTimestamp,
    TimeSpan MoveNextWaitTime,
    TimeSpan ConsumerGapTime,
    int ExceptionCount,
    string? ExceptionType,
    string? ExceptionMessage,
    bool IsTimingEstimated,
    long TimedMoveNextCalls,
    long UntimedMoveNextCalls);
