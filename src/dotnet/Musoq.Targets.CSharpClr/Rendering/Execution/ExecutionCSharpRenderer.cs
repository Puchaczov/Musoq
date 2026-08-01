using System.Collections.Generic;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{

    private const string DescSchemaVariableName = "descSchema";
    private const string DescRuntimeContextVariableName = "descRuntimeCtx";
    private const string DescEmptyInferredColumnsVariableName = "emptyInferred";
    private const string DescSchemaTableVariableName = "schemaTable";
    private const string StatsVariableName = "stats";
    private const string ProfileRecorderVariableName = "profileRecorder";
    private readonly ExecutionRenderOptions _renderOptions;
    private int _dynamicResolverValueSequence;
    private IReadOnlyList<ScriptParameterDefinition> _scriptParameterDefinitions => _renderOptions.ScriptParameterDefinitions;
    private IReadOnlyList<ScriptVariableDefinition> _scriptVariableDefinitions => _renderOptions.ScriptVariableDefinitions;
    private IReadOnlyDictionary<string, string> _scriptParameterLocalNames => _renderOptions.ScriptParameterLocalNames;
    private IReadOnlyDictionary<string, string> _scriptVariableLocalNames => _renderOptions.ScriptVariableLocalNames;
    private QueryInstrumentationMode _instrumentationMode => _renderOptions.InstrumentationMode;

    private bool IsInstrumentationEnabled => _instrumentationMode != QueryInstrumentationMode.Disabled;

    private bool IsFullInstrumentationEnabled => _instrumentationMode == QueryInstrumentationMode.Full;

    private bool IsOperatorProfilingEnabledFor(ExecutionRenderContext context) => IsFullInstrumentationEnabled && context.Session.ProfileRecorderInScope;

    internal bool IsFullProfilingEnabledForGeneratedCode => IsFullInstrumentationEnabled;
}
