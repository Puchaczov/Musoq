using System.Collections.Generic;

namespace Musoq.Evaluator;

public interface IParameterizedRunnable
{
    IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; }

    IReadOnlyList<ScriptParameterContract> ParameterContracts { get; }
}
