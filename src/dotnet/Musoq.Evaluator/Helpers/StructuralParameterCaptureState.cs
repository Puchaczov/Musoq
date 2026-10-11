using System.Threading;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Helpers;

/// <summary>Execution-owned state used while a generated parameter snapshot is built.</summary>
public sealed class StructuralParameterCaptureState
{
    private readonly StructuralInputLimits _limits;
    private long _nodes;
    private long _stringBytes;
    private int _maxDepth;

    /// <summary>Creates a capture state using the global structural input limits.</summary>
    public StructuralParameterCaptureState(
        CancellationToken cancellationToken,
        StructuralInputLimits? limits = null)
    {
        CancellationToken = cancellationToken;
        _limits = limits ?? StructuralInputLimits.Default;
    }

    /// <summary>Gets the cancellation token used by this capture.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Gets the measured usage accumulated by this capture state.</summary>
    public StructuralInputMetrics Metrics => new(_maxDepth, _nodes, _stringBytes);

    /// <summary>Reserves one logical value node at the supplied one-based depth.</summary>
    public void ReserveNode(int depth)
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (depth <= 0 || depth > _limits.MaxDepth)
            throw new InvalidOperationException(
                $"Structural input exceeds maximum depth {_limits.MaxDepth}.");

        _nodes = checked(_nodes + 1);
        if (_nodes > _limits.MaxNodes)
            throw new InvalidOperationException(
                $"Structural input exceeds maximum value nodes {_limits.MaxNodes}.");
        if (depth > _maxDepth)
            _maxDepth = depth;
    }

    /// <summary>Reserves a precomputed constant subtree before its carrier is materialized.</summary>
    public void ReserveMetrics(StructuralInputMetrics metrics, int rootDepth)
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (rootDepth <= 0)
            throw new ArgumentOutOfRangeException(nameof(rootDepth));
        var maxDepth = checked(rootDepth + Math.Max(0, metrics.MaxDepth) - 1);
        if (maxDepth > _limits.MaxDepth)
            throw new InvalidOperationException(
                $"Structural input exceeds maximum depth {_limits.MaxDepth}.");
        if (metrics.NodeCount < 0 || metrics.StringBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(metrics));
        _nodes = checked(_nodes + metrics.NodeCount);
        _stringBytes = checked(_stringBytes + metrics.StringBytes);
        if (_nodes > _limits.MaxNodes)
            throw new InvalidOperationException(
                $"Structural input exceeds maximum value nodes {_limits.MaxNodes}.");
        if (_stringBytes > _limits.MaxStringBytes)
            throw new InvalidOperationException(
                $"Structural input exceeds maximum string payload {_limits.MaxStringBytes} bytes.");
        if (maxDepth > _maxDepth)
            _maxDepth = maxDepth;
    }

    /// <summary>Checks a known collection lower bound before allocating its result storage.</summary>
    public void EnsureCollectionLowerBound(int count)
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (count < 0)
            throw new InvalidOperationException("Structural collection count cannot be negative.");
        if (count > _limits.MaxNodes - _nodes)
            throw new InvalidOperationException("Structural input exceeds maximum value nodes.");
    }

    /// <summary>Charges a string's UTF-16 payload against the global limit.</summary>
    public void ReserveString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        CancellationToken.ThrowIfCancellationRequested();

        try
        {
            _stringBytes = checked(_stringBytes + checked((long)value.Length * sizeof(char)));
        }
        catch (OverflowException exception)
        {
            throw new InvalidOperationException(
                "Structural input string payload overflowed the configured limit.",
                exception);
        }

        if (_stringBytes > _limits.MaxStringBytes)
            throw new InvalidOperationException(
                $"Structural input exceeds maximum string payload {_limits.MaxStringBytes} bytes.");
    }

}
