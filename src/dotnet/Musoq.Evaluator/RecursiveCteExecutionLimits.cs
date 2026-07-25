namespace Musoq.Evaluator;

public sealed record RecursiveCteExecutionLimits
{
    public RecursiveCteExecutionLimits(
        int maxIterations = 1_000,
        int maxRows = 10_000_000)
        : this(maxIterations, maxRows, 10_000_000)
    {
    }

    public RecursiveCteExecutionLimits(
        int maxIterations,
        int maxRows,
        int maxSnapshotRows)
    {
        MaxIterations = maxIterations > 0
            ? maxIterations
            : throw new ArgumentOutOfRangeException(
                nameof(maxIterations),
                maxIterations,
                "Recursive CTE maximum iterations must be a positive integer.");
        MaxRows = maxRows > 0
            ? maxRows
            : throw new ArgumentOutOfRangeException(
                nameof(maxRows),
                maxRows,
                "Recursive CTE maximum rows must be a positive integer.");
        MaxSnapshotRows = maxSnapshotRows > 0
            ? maxSnapshotRows
            : throw new ArgumentOutOfRangeException(
                nameof(maxSnapshotRows),
                maxSnapshotRows,
                "Recursive CTE maximum snapshot rows must be a positive integer.");
    }

    public int MaxIterations { get; }

    public int MaxRows { get; }

    public int MaxSnapshotRows { get; }
}
