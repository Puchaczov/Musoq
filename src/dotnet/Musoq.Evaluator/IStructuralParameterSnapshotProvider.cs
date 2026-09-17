using System.Collections.Generic;
using System.Threading;

namespace Musoq.Evaluator;

/// <summary>
/// Captures declared structural parameters at the execution boundary.
/// Implementations own the shape-specific normalization and return values that
/// are safe for the generated execution body to retain for that run.
/// </summary>
public interface IStructuralParameterSnapshotProvider
{
    /// <summary>
    /// Validates and snapshots supplied parameters before query sources open.
    /// </summary>
    /// <param name="supplied">The host values supplied for this execution.</param>
    /// <param name="cancellationToken">The execution cancellation token.</param>
    /// <returns>An owned, immutable parameter dictionary for the execution.</returns>
    IReadOnlyDictionary<string, object?> CaptureParameterSnapshot(
        IReadOnlyDictionary<string, object?> supplied,
        CancellationToken cancellationToken);
}
