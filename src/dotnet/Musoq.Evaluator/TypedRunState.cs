using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Schema;

namespace Musoq.Evaluator;

public sealed class TypedRunState
{
    private readonly IDictionary<string, object?> _parameters;
    private readonly IReadOnlyList<ScriptParameterDefinition> _parameterDefinitions;
    private readonly IReadOnlyList<ScriptParameterDefinition> _requiredParameters;
    private event QueryPhaseEventHandler? PhaseChangedHandlers;
    private event DataSourceEventHandler? DataSourceProgressHandlers;

    public TypedRunState(
        IReadOnlyList<ScriptParameterDefinition>? parameterDefinitions = null,
        IDictionary<string, object?>? parameters = null)
    {
        _parameters = parameters ?? new Dictionary<string, object?>(StringComparer.Ordinal);
        _parameterDefinitions = parameterDefinitions?.ToArray() ?? Array.Empty<ScriptParameterDefinition>();
        _requiredParameters = _parameterDefinitions
            .Where(static definition => definition.IsRequired)
            .ToArray();
    }

    public IDictionary<string, object?> Parameters => _parameters;

    public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions => _parameterDefinitions;

    public IReadOnlyList<ScriptParameterDefinition> RequiredParameters => _requiredParameters;

    public void AddPhaseChanged(QueryPhaseEventHandler? handler)
    {
        PhaseChangedHandlers += handler;
    }

    public void RemovePhaseChanged(QueryPhaseEventHandler? handler)
    {
        PhaseChangedHandlers -= handler;
    }

    public void AddDataSourceProgress(DataSourceEventHandler? handler)
    {
        DataSourceProgressHandlers += handler;
    }

    public void RemoveDataSourceProgress(DataSourceEventHandler? handler)
    {
        DataSourceProgressHandlers -= handler;
    }

    public TypedQueryRunOptions CreateOptions(CancellationToken token)
    {
        return new TypedQueryRunOptions(
            token,
            ParameterSnapshot.CaptureMutableOrEmpty(_parameters),
            PhaseChangedHandlers,
            DataSourceProgressHandlers);
    }
}
