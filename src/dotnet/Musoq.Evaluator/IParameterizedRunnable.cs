using System.Collections.Generic;

namespace Musoq.Evaluator;

public interface IParameterizedRunnable
{
    IDictionary<string, object?> Parameters { get; }

    IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; }

    IReadOnlyList<ScriptParameterContract> ParameterContracts { get; }
}
