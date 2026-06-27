using System;
using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Converter;

public interface ICompiledTypedQueryArtifact
{
    int ArtifactVersion { get; }

    string EngineVersion { get; }

    string RuntimeVersion { get; }

    byte[] DllFile { get; }

    byte[]? PdbFile { get; }

    string RunnableTypeName { get; }

    QueryResultMode ResultMode { get; }

    Type OutputType { get; }

    string OutputTypeName { get; }

    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; }

    IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; }

    IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; }

    IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; }

    IReadOnlyList<TypedArtifactSourceSlotIdentity> SourceSlotIdentities { get; }
}
