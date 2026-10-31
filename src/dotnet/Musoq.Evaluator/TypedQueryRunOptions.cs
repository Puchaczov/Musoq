using System.Threading;
using Musoq.Schema;

namespace Musoq.Evaluator;

public sealed class TypedQueryRunOptions
{
    public TypedQueryRunOptions()
    {
    }

    public TypedQueryRunOptions(CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
    }

    public TypedQueryRunOptions(
        CancellationToken cancellationToken,
        TypedParameterSnapshot? parameters,
        QueryPhaseEventHandler? phaseChanged = null,
        DataSourceEventHandler? dataSourceProgress = null,
        QueryProgressEventHandler? queryProgress = null,
        QueryProgressOptions? queryProgressOptions = null)
    {
        CancellationToken = cancellationToken;
        TypedParameters = parameters ?? TypedParameterSnapshot.Empty;
        PhaseChanged = phaseChanged;
        DataSourceProgress = dataSourceProgress;
        QueryProgress = queryProgress;
        QueryProgressOptions = queryProgressOptions;
    }

    public CancellationToken CancellationToken { get; init; }

    /// <summary>Gets a snapshot retaining each supplied parameter's concrete type.</summary>
    public TypedParameterSnapshot TypedParameters { get; init; } = TypedParameterSnapshot.Empty;

    public QueryPhaseEventHandler? PhaseChanged { get; init; }

    public DataSourceEventHandler? DataSourceProgress { get; init; }

    public QueryProgressEventHandler? QueryProgress { get; init; }

    public QueryProgressOptions? QueryProgressOptions { get; init; }

}
