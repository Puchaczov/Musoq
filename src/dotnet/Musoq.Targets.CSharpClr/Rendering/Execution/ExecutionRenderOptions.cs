using System.Collections.Generic;
using System.Linq;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

internal sealed class ExecutionRenderOptions
{
    private ExecutionRenderOptions(
        IReadOnlyList<ScriptParameterDefinition> scriptParameterDefinitions,
        IReadOnlyList<ScriptVariableDefinition> scriptVariableDefinitions,
        IReadOnlyDictionary<string, string> scriptParameterLocalNames,
        IReadOnlyDictionary<string, string> scriptVariableLocalNames,
        QueryInstrumentationMode instrumentationMode,
        CSharpClrExecutionBindingContext executionBindings,
        string generatedMemberSuffix)
    {
        ScriptParameterDefinitions = scriptParameterDefinitions;
        ScriptVariableDefinitions = scriptVariableDefinitions;
        ScriptParameterLocalNames = scriptParameterLocalNames;
        ScriptVariableLocalNames = scriptVariableLocalNames;
        InstrumentationMode = instrumentationMode;
        ExecutionBindings = executionBindings;
        GeneratedMemberSuffix = generatedMemberSuffix;
    }

    internal IReadOnlyList<ScriptParameterDefinition> ScriptParameterDefinitions { get; }

    internal IReadOnlyList<ScriptVariableDefinition> ScriptVariableDefinitions { get; }

    internal IReadOnlyDictionary<string, string> ScriptParameterLocalNames { get; }

    internal IReadOnlyDictionary<string, string> ScriptVariableLocalNames { get; }

    internal QueryInstrumentationMode InstrumentationMode { get; }

    internal CSharpClrExecutionBindingContext ExecutionBindings { get; }

    internal string GeneratedMemberSuffix { get; }

    internal static ExecutionRenderOptions Create(
        IReadOnlyList<ScriptParameterDefinition>? scriptParameterDefinitions,
        IReadOnlyList<ScriptVariableDefinition>? scriptVariableDefinitions,
        QueryInstrumentationMode instrumentationMode,
        CSharpClrExecutionBindingContext? executionBindings = null,
        string generatedMemberSuffix = "")
    {
        var parameterDefinitions = (scriptParameterDefinitions ?? []).ToArray();
        var variableDefinitions = (scriptVariableDefinitions ?? []).ToArray();

        return new ExecutionRenderOptions(
            parameterDefinitions,
            variableDefinitions,
            ScriptParameterLocalNameResolver.CreateLocalNameMap(parameterDefinitions),
            ScriptVariableLocalNameResolver.CreateLocalNameMap(variableDefinitions),
            instrumentationMode,
            executionBindings ?? new CSharpClrExecutionBindingContext(),
            generatedMemberSuffix);
    }
}
