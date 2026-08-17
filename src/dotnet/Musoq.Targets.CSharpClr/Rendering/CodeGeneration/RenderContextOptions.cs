using System.Collections.Generic;
using Musoq.Evaluator.Utils;

namespace Musoq.Targets.CSharpClr;

public sealed record RenderContextOptions(
    Scope? Scope = null,
    string AssemblyName = "",
    IReadOnlyList<ScriptParameterDefinition>? ScriptParameterDefinitions = null,
    IReadOnlyList<ScriptVariableDefinition>? ScriptVariableDefinitions = null,
    QueryInstrumentationMode InstrumentationMode = QueryInstrumentationMode.Disabled,
    QueryResultMode ResultMode = QueryResultMode.Table,
    Type? OutputType = null,
    FinalResultSinkKind FinalResultSinkKind = FinalResultSinkKind.TableDirect,
    bool ForceTableResultMaterialization = false,
    bool EnableContextualExecution = false);
