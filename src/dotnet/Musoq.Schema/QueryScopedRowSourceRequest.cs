using Musoq.Schema.Optimization;

namespace Musoq.Schema;

/// <summary>
/// Immutable execution context and logical shape supplied to a query-scoped row source.
/// </summary>
public sealed record QueryScopedRowSourceRequest
{
    public QueryScopedRowSourceRequest(SourceExecutionContext executionContext, QueryRowShape shape)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(shape);

        ExecutionContext = executionContext;
        Shape = shape;
    }

    public SourceExecutionContext ExecutionContext { get; }

    public QueryRowShape Shape { get; }
}
