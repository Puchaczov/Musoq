using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private readonly ScriptParameterMetadataBinder _scriptParameters;
    private readonly ScriptVariableMetadataBinder _scriptVariables;

    public IReadOnlyList<ScriptParameterDefinition> ScriptParameterDefinitions =>
        _scriptParameters.Definitions.ToArray();

    public IReadOnlyList<ScriptVariableDefinition> ScriptVariableDefinitions =>
        _scriptVariables.Definitions.ToArray();
}
