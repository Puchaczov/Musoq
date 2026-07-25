using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Schema;

namespace Musoq.Evaluator;

public sealed class TypedRunState
{
    private readonly object _runtimeGate = new();
    private readonly SynchronizedParameterDictionary _parameters;
    private readonly IReadOnlyList<ScriptParameterDefinition> _parameterDefinitions;
    private readonly IReadOnlyList<ScriptParameterContract> _parameterContracts;
    private readonly IReadOnlyList<ScriptParameterDefinition> _requiredParameters;
    private event QueryPhaseEventHandler? PhaseChangedHandlers;
    private event DataSourceEventHandler? DataSourceProgressHandlers;

    public TypedRunState(
        IReadOnlyList<ScriptParameterDefinition>? parameterDefinitions = null,
        IDictionary<string, object?>? parameters = null)
    {
        _parameters = new SynchronizedParameterDictionary(
            _runtimeGate,
            parameters);
        _parameterDefinitions = parameterDefinitions?.ToArray() ?? Array.Empty<ScriptParameterDefinition>();
        _parameterContracts = _parameterDefinitions
            .Select(static definition => definition.Contract)
            .ToArray();
        _requiredParameters = _parameterDefinitions
            .Where(static definition => definition.IsRequired)
            .ToArray();
    }

    public IDictionary<string, object?> Parameters => _parameters;

    public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions => _parameterDefinitions;

    public IReadOnlyList<ScriptParameterContract> ParameterContracts => _parameterContracts;

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
            _parameters.Snapshot(),
            PhaseChangedHandlers,
            DataSourceProgressHandlers);
    }
}
