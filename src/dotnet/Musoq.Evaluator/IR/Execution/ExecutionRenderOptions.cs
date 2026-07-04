using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

internal sealed class ExecutionRenderOptions
{
    private ExecutionRenderOptions(
        IReadOnlyList<ScriptParameterDefinition> scriptParameterDefinitions,
        IReadOnlyList<ScriptVariableDefinition> scriptVariableDefinitions,
        IReadOnlyDictionary<string, string> scriptParameterLocalNames,
        IReadOnlyDictionary<string, string> scriptVariableLocalNames,
        QueryInstrumentationMode instrumentationMode)
    {
        ScriptParameterDefinitions = scriptParameterDefinitions;
        ScriptVariableDefinitions = scriptVariableDefinitions;
        ScriptParameterLocalNames = scriptParameterLocalNames;
        ScriptVariableLocalNames = scriptVariableLocalNames;
        InstrumentationMode = instrumentationMode;
    }

    internal IReadOnlyList<ScriptParameterDefinition> ScriptParameterDefinitions { get; }

    internal IReadOnlyList<ScriptVariableDefinition> ScriptVariableDefinitions { get; }

    internal IReadOnlyDictionary<string, string> ScriptParameterLocalNames { get; }

    internal IReadOnlyDictionary<string, string> ScriptVariableLocalNames { get; }

    internal QueryInstrumentationMode InstrumentationMode { get; }

    internal static ExecutionRenderOptions Create(
        IReadOnlyList<ScriptParameterDefinition>? scriptParameterDefinitions,
        IReadOnlyList<ScriptVariableDefinition>? scriptVariableDefinitions,
        QueryInstrumentationMode instrumentationMode)
    {
        var parameterDefinitions = (scriptParameterDefinitions ?? []).ToArray();
        var variableDefinitions = (scriptVariableDefinitions ?? []).ToArray();

        return new ExecutionRenderOptions(
            parameterDefinitions,
            variableDefinitions,
            ScriptParameterLocalNameResolver.CreateLocalNameMap(parameterDefinitions),
            ScriptVariableLocalNameResolver.CreateLocalNameMap(variableDefinitions),
            instrumentationMode);
    }
}
