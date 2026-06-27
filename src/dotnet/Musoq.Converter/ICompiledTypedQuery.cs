using System.Collections.Generic;
using System.Threading;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Converter;

public interface ICompiledTypedQuery<out TOut>
{
    event QueryPhaseEventHandler PhaseChanged;

    event DataSourceEventHandler DataSourceProgress;

    IDictionary<string, object?> Parameters { get; }

    IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; }

    IReadOnlyList<ScriptParameterDefinition> RequiredParameters { get; }

    TypedQueryDiagnostics Diagnostics { get; }

    IEnumerable<TOut> Run(CancellationToken token, params MusoqSourceRows[] sources);

    IEnumerable<TOut> Run(TypedQueryRunOptions options, params MusoqSourceRows[] sources);
}
