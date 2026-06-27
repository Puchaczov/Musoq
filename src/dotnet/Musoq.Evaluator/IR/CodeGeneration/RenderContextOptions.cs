using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Utils;

namespace Musoq.Evaluator.IR.CodeGeneration;

public sealed record RenderContextOptions(
    Scope? Scope = null,
    string AssemblyName = "",
    IReadOnlyList<ScriptParameterDefinition>? ScriptParameterDefinitions = null,
    IReadOnlyList<ScriptVariableDefinition>? ScriptVariableDefinitions = null,
    QueryInstrumentationMode InstrumentationMode = QueryInstrumentationMode.Disabled,
    QueryResultMode ResultMode = QueryResultMode.Table,
    Type? OutputType = null,
    FinalResultSinkKind FinalResultSinkKind = FinalResultSinkKind.TableDirect,
    bool ForceTableResultMaterialization = false);
