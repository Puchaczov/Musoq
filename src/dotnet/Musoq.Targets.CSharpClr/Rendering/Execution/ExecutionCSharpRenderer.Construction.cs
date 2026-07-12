using System.Collections.Generic;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    public ExecutionCSharpRenderer(
        IReadOnlyList<ScriptParameterDefinition>? scriptParameterDefinitions = null,
        IReadOnlyList<ScriptVariableDefinition>? scriptVariableDefinitions = null,
        QueryInstrumentationMode instrumentationMode = QueryInstrumentationMode.Disabled)
    {
        _renderOptions = ExecutionRenderOptions.Create(
            scriptParameterDefinitions,
            scriptVariableDefinitions,
            instrumentationMode);
    }
}
