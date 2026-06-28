using System.Collections.Generic;
using System.Threading;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Converter;

public interface ICompiledTypedProfileQuery<TOut>
{
    event QueryPhaseEventHandler PhaseChanged;

    event DataSourceEventHandler DataSourceProgress;

    IDictionary<string, object?> Parameters { get; }

    IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; }

    IReadOnlyList<ScriptParameterContract> ParameterContracts { get; }

    IReadOnlyList<ScriptParameterDefinition> RequiredParameters { get; }

    TypedQueryDiagnostics Diagnostics { get; }

    TypedQueryProfileResult<TOut> RunWithProfile(CancellationToken token, params MusoqSourceRows[] sources);

    TypedQueryProfileResult<TOut> RunWithProfile(TypedQueryRunOptions options, params MusoqSourceRows[] sources);
}
