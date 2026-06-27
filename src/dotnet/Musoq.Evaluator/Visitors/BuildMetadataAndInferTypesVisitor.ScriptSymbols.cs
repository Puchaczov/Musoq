using System.Collections.Generic;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private readonly ScriptParameterMetadataBinder _scriptParameters;
    private readonly ScriptVariableMetadataBinder _scriptVariables;

    public IReadOnlyList<ScriptParameterDefinition> ScriptParameterDefinitions => _scriptParameters.Definitions;

    public IReadOnlyList<ScriptVariableDefinition> ScriptVariableDefinitions => _scriptVariables.Definitions;
}