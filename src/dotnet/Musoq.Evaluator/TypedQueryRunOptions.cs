using System.Collections.Generic;
using System.Threading;
using Musoq.Schema;

namespace Musoq.Evaluator;

public sealed class TypedQueryRunOptions
{
    private readonly IReadOnlyDictionary<string, object?>? _parameters;

    public TypedQueryRunOptions()
    {
    }

    public TypedQueryRunOptions(CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
    }

    public TypedQueryRunOptions(
        CancellationToken cancellationToken,
        IEnumerable<KeyValuePair<string, object?>>? parameters,
        QueryPhaseEventHandler? phaseChanged = null,
        DataSourceEventHandler? dataSourceProgress = null)
    {
        CancellationToken = cancellationToken;
        Parameters = ParameterSnapshot.CaptureReadOnlyOrNull(parameters);
        PhaseChanged = phaseChanged;
        DataSourceProgress = dataSourceProgress;
    }

    public CancellationToken CancellationToken { get; init; }

    public IReadOnlyDictionary<string, object?>? Parameters
    {
        get => _parameters;
        init => _parameters = ParameterSnapshot.CaptureReadOnlyOrNull(value);
    }

    public QueryPhaseEventHandler? PhaseChanged { get; init; }

    public DataSourceEventHandler? DataSourceProgress { get; init; }
}
