using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Schema;

namespace Musoq.Evaluator;

public sealed class TypedRunState
{
    private readonly object _runtimeGate = new();
    private TypedParameterValues? _typedParameters;
    private readonly IReadOnlyList<ScriptParameterDefinition> _parameterDefinitions;
    private readonly IReadOnlyList<ScriptParameterContract> _parameterContracts;
    private readonly IReadOnlyList<ScriptParameterDefinition> _requiredParameters;
    private event QueryPhaseEventHandler? PhaseChangedHandlers;
    private event DataSourceEventHandler? DataSourceProgressHandlers;
    private event QueryProgressEventHandler? QueryProgressHandlers;

    public TypedRunState(
        IReadOnlyList<ScriptParameterDefinition>? parameterDefinitions = null,
        TypedParameterSnapshot? parameters = null)
    {
        if (parameters is not null)
            TypedParameters.ReplaceWith(parameters);
        _parameterDefinitions = parameterDefinitions?.ToArray() ?? Array.Empty<ScriptParameterDefinition>();
        _parameterContracts = _parameterDefinitions
            .Select(static definition => definition.Contract)
            .ToArray();
        _requiredParameters = _parameterDefinitions
            .Where(static definition => definition.IsRequired)
            .ToArray();
    }

    /// <summary>Gets typed slots for the next execution.</summary>
    public TypedParameterValues TypedParameters
    {
        get { lock (_runtimeGate) return _typedParameters ??= new TypedParameterValues(_runtimeGate, null); }
    }

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

    public void AddQueryProgress(QueryProgressEventHandler? handler)
    {
        QueryProgressHandlers += handler;
    }

    public void RemoveQueryProgress(QueryProgressEventHandler? handler)
    {
        QueryProgressHandlers -= handler;
    }

    public TypedQueryRunOptions CreateOptions(CancellationToken token)
    {
        return new TypedQueryRunOptions(
            token,
            CaptureTypedParameters(),
            PhaseChangedHandlers,
            DataSourceProgressHandlers,
            QueryProgressHandlers,
            null)
        {
            TypedParameters = CaptureTypedParameters()
        };
    }

    private TypedParameterSnapshot CaptureTypedParameters()
    {
        lock (_runtimeGate) return _typedParameters?.Capture() ?? TypedParameterSnapshot.Empty;
    }
}
