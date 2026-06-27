using System.Collections.Generic;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    public ExecutionCSharpRenderer(
        IReadOnlyList<ScriptParameterDefinition>? scriptParameterDefinitions = null,
        IReadOnlyList<ScriptVariableDefinition>? scriptVariableDefinitions = null,
        QueryInstrumentationMode instrumentationMode = QueryInstrumentationMode.Disabled)
    {
        _scriptParameterDefinitions = scriptParameterDefinitions ?? Array.Empty<ScriptParameterDefinition>();
        _scriptVariableDefinitions = scriptVariableDefinitions ?? Array.Empty<ScriptVariableDefinition>();
        _scriptParameterLocalNames = ScriptParameterLocalNameResolver.CreateLocalNameMap(_scriptParameterDefinitions);
        _scriptVariableLocalNames = ScriptVariableLocalNameResolver.CreateLocalNameMap(_scriptVariableDefinitions);
        _instrumentationMode = instrumentationMode;
    }
}
