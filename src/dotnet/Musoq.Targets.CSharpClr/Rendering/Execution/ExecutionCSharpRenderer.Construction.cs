using System.Collections.Generic;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    public ExecutionCSharpRenderer(
        IReadOnlyList<ScriptParameterDefinition>? scriptParameterDefinitions = null,
        IReadOnlyList<ScriptVariableDefinition>? scriptVariableDefinitions = null,
        QueryInstrumentationMode instrumentationMode = QueryInstrumentationMode.Disabled)
        : this(
            scriptParameterDefinitions,
            scriptVariableDefinitions,
            instrumentationMode,
            new CSharpClrExecutionBindingContext())
    {
    }

    internal ExecutionCSharpRenderer(
        IReadOnlyList<ScriptParameterDefinition>? scriptParameterDefinitions,
        IReadOnlyList<ScriptVariableDefinition>? scriptVariableDefinitions,
        QueryInstrumentationMode instrumentationMode,
        CSharpClrExecutionBindingContext executionBindings,
        string generatedMemberSuffix = "")
    {
        _renderOptions = ExecutionRenderOptions.Create(
            scriptParameterDefinitions,
            scriptVariableDefinitions,
            instrumentationMode,
            executionBindings,
            generatedMemberSuffix);
    }
}
