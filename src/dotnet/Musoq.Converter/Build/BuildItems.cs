using System.Collections.Generic;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.Optimization;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Converter.Build;

/// <summary>
/// Public build artifact bag retained for compatibility with existing callers.
/// </summary>
/// <remarks>
/// The raw dictionary surface is a legacy compatibility shell. Production pipeline code should use
/// typed properties, typed stage artifacts, or explicit stage contexts instead of treating this as an
/// active string-keyed contract.
/// </remarks>
public partial class BuildItems : Dictionary<string, object>
{
    public byte[]? DllFile
    {
        get => GetDllFileValue();
        set => SetOptional(BuildItemKeys.DllFile, value is null ? null : (byte[])value.Clone());
    }

    public byte[]? PdbFile
    {
        get => GetPdbFileValue();
        set => SetOptional(BuildItemKeys.PdbFile, value is null ? null : (byte[])value.Clone());
    }

    public RootNode TransformedQueryTree
    {
        get => GetRequired<RootNode>(BuildItemKeys.TransformedQueryTree);
        set => SetRequired(BuildItemKeys.TransformedQueryTree, value);
    }

    public RootNode RawQueryTree
    {
        get => GetRequired<RootNode>(BuildItemKeys.RawQueryTree);
        set => SetRequired(BuildItemKeys.RawQueryTree, value);
    }

    internal SemanticPhaseArtifacts? SemanticPhaseArtifacts
    {
        get => GetOptional<SemanticPhaseArtifacts>(BuildItemKeys.SemanticPhaseArtifacts);
        set => SetOptional(BuildItemKeys.SemanticPhaseArtifacts, value);
    }

    public string RawQuery
    {
        get => TryGetArtifact<string>(BuildItemKeys.RawQuery, out var str)
            ? str
            : throw AstValidationException.ForInvalidNodeStructure("BuildItems", "RawQuery access",
                "RawQuery is not set or is null");
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw AstValidationException.ForInvalidNodeStructure("BuildItems", "RawQuery setting",
                    "RawQuery cannot be null or whitespace");
            SetRequired(BuildItemKeys.RawQuery, value);
        }
    }

    public string AssemblyName
    {
        get => GetRequired<string>(BuildItemKeys.AssemblyName);
        set => SetRequired(BuildItemKeys.AssemblyName, value);
    }

    public ISchemaProvider SchemaProvider
    {
        get => GetRequired<ISchemaProvider>(BuildItemKeys.SchemaProvider);
        set => SetRequired(BuildItemKeys.SchemaProvider, value);
    }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId
    {
        get => GetRequired<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>>(BuildItemKeys.SourceRuntimeSettingsBySourceContextId);
        set => SetRequired(BuildItemKeys.SourceRuntimeSettingsBySourceContextId, value);
    }

    public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId
    {
        get => GetRequired<IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>>(BuildItemKeys.SourceRuntimeSettingDescriptionsBySourceContextId);
        set => SetRequired(BuildItemKeys.SourceRuntimeSettingDescriptionsBySourceContextId, value);
    }

    public bool HasDeclaredSourceRuntimeSettings
    {
        get => GetFlag(BuildItemKeys.HasDeclaredSourceRuntimeSettings, defaultWhenMissing: false);
        set => SetFlag(BuildItemKeys.HasDeclaredSourceRuntimeSettings, value);
    }

    public bool HasSourceRuntimeSettingValues
    {
        get => GetFlag(BuildItemKeys.HasSourceRuntimeSettingValues, defaultWhenMissing: false);
        set => SetFlag(BuildItemKeys.HasSourceRuntimeSettingValues, value);
    }

    public EmitResult EmitResult
    {
        get => GetRequired<EmitResult>(BuildItemKeys.EmitResult);
        set => SetRequired(BuildItemKeys.EmitResult, value);
    }

    internal TargetFinalizationResult? FinalizationResult
    {
        get => GetOptional<TargetFinalizationResult>(BuildItemKeys.FinalizationResult);
        set => SetOptional(BuildItemKeys.FinalizationResult, value);
    }

    public IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]> UsedColumns
    {
        get => GetRequired<IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]>>(BuildItemKeys.UsedColumns);
        set => SetRequired(BuildItemKeys.UsedColumns, value);
    }

    public IReadOnlyDictionary<SchemaFromNode, WhereNode> UsedWhereNodes
    {
        get => GetRequired<IReadOnlyDictionary<SchemaFromNode, WhereNode>>(BuildItemKeys.UsedWhereNodes);
        set => SetRequired(BuildItemKeys.UsedWhereNodes, value);
    }

    public IReadOnlyDictionary<SchemaFromNode, SourcePlanRequest> SourcePlanRequestsPerSchema
    {
        get => GetRequired<IReadOnlyDictionary<SchemaFromNode, SourcePlanRequest>>(BuildItemKeys.SourcePlanRequestsPerSchema);
        set => SetRequired(BuildItemKeys.SourcePlanRequestsPerSchema, value);
    }

    internal IReadOnlyDictionary<SchemaFromNode, SourceContractDiagnosticLocationMap> SourceContractDiagnosticLocationsPerSchema
    {
        get => GetOptional<IReadOnlyDictionary<SchemaFromNode, SourceContractDiagnosticLocationMap>>(
                   BuildItemKeys.SourceContractDiagnosticLocationsPerSchema) ??
               new Dictionary<SchemaFromNode, SourceContractDiagnosticLocationMap>();
        set => SetRequired(BuildItemKeys.SourceContractDiagnosticLocationsPerSchema, value);
    }

    public IReadOnlyList<ScriptParameterDefinition> ScriptParameterDefinitions
    {
        get => GetListOrEmpty<ScriptParameterDefinition>(BuildItemKeys.ScriptParameterDefinitions);
        set => SetRequired(BuildItemKeys.ScriptParameterDefinitions, value);
    }

    public Func<ISchemaProvider, IReadOnlyDictionary<string, string[]>, CompilationOptions, SchemaRegistry?, ILogger<BuildMetadataAndInferTypesVisitor>, BuildMetadataAndInferTypesVisitor>?
        CreateBuildMetadataAndInferTypesVisitor
    {
        get => GetOptional<Func<ISchemaProvider, IReadOnlyDictionary<string, string[]>, CompilationOptions, SchemaRegistry?, ILogger<BuildMetadataAndInferTypesVisitor>, BuildMetadataAndInferTypesVisitor>>(
            BuildItemKeys.CreateBuildMetadataAndInferTypesVisitor);
        set => SetOptional(BuildItemKeys.CreateBuildMetadataAndInferTypesVisitor, value);
    }

    public CompilationOptions CompilationOptions
    {
        get
        {
            if (!ContainsArtifact<CompilationOptions>(BuildItemKeys.CompilationOptions))
                SetRequired(BuildItemKeys.CompilationOptions, new CompilationOptions(ParallelizationMode.Full));

            return GetRequired<CompilationOptions>(BuildItemKeys.CompilationOptions);
        }
        set => SetRequired(BuildItemKeys.CompilationOptions, value);
    }

    public SchemaRegistry? SchemaRegistry
    {
        get => GetOptional<SchemaRegistry>(BuildItemKeys.SchemaRegistry);
        set => SetOptional(BuildItemKeys.SchemaRegistry, value);
    }

    public string? InterpreterSourceCode
    {
        get => GetOptional<string>(BuildItemKeys.InterpreterSourceCode);
        set => SetOptional(BuildItemKeys.InterpreterSourceCode, value);
    }

    public CteExecutionPlan? CteExecutionPlan
    {
        get => GetOptional<CteExecutionPlan>(BuildItemKeys.CteExecutionPlan);
        set => SetOptional(BuildItemKeys.CteExecutionPlan, value);
    }

    public LogicalNode? LogicalPlan
    {
        get => GetOptional<LogicalNode>(BuildItemKeys.LogicalPlan);
        set => SetOptional(BuildItemKeys.LogicalPlan, value);
    }

    public PhysicalNode? PhysicalPlan
    {
        get => GetOptional<PhysicalNode>(BuildItemKeys.PhysicalPlan);
        set => SetOptional(BuildItemKeys.PhysicalPlan, value);
    }

    internal PlanningResult? PlanningResult
    {
        get => GetOptional<PlanningResult>(BuildItemKeys.PlanningResult);
        set => SetOptional(BuildItemKeys.PlanningResult, value);
    }

    public string? PlanningText
    {
        get => GetOptional<string>(BuildItemKeys.PlanningText);
        set => SetOptional(BuildItemKeys.PlanningText, value);
    }

    public ExecutionPlan? ExecutionPlan
    {
        get => GetOptional<ExecutionPlan>(BuildItemKeys.ExecutionPlan);
        set => SetOptional(BuildItemKeys.ExecutionPlan, value);
    }

    public ExecutionPlanBuildResult? ExecutionPlanBuildResult
    {
        get => GetOptional<ExecutionPlanBuildResult>(BuildItemKeys.ExecutionPlanBuildResult);
        set => SetOptional(BuildItemKeys.ExecutionPlanBuildResult, value);
    }

    public string? ExecutionPlanText
    {
        get => GetOptional<string>(BuildItemKeys.ExecutionPlanText);
        set => SetOptional(BuildItemKeys.ExecutionPlanText, value);
    }

    internal ExecutionTargetCompatibilityReport? ExecutionTargetCompatibilityReport { get => GetOptional<ExecutionTargetCompatibilityReport>(BuildItemKeys.ExecutionTargetCompatibilityReport); set => SetOptional(BuildItemKeys.ExecutionTargetCompatibilityReport, value); }

    internal TargetRuntimeContract? TargetRuntimeContract { get => GetOptional<TargetRuntimeContract>(BuildItemKeys.TargetRuntimeContract); set => SetOptional(BuildItemKeys.TargetRuntimeContract, value); }

    internal ExecutionTargetReadinessReport? ExecutionTargetReadinessReport { get => GetOptional<ExecutionTargetReadinessReport>(BuildItemKeys.ExecutionTargetReadinessReport); set => SetOptional(BuildItemKeys.ExecutionTargetReadinessReport, value); }

    internal ExecutionSemanticsContract? ExecutionSemanticsContract { get => GetOptional<ExecutionSemanticsContract>(BuildItemKeys.ExecutionSemanticsContract); set => SetOptional(BuildItemKeys.ExecutionSemanticsContract, value); }


    public SourceText? SourceText
    {
        get => GetOptional<SourceText>(BuildItemKeys.SourceText);
        set => SetOptional(BuildItemKeys.SourceText, value);
    }

    public DiagnosticContext DiagnosticContext
    {
        get => GetRequired<DiagnosticContext>(BuildItemKeys.DiagnosticContext);
        init => SetRequired(BuildItemKeys.DiagnosticContext, value);
    }

    public bool EmitPdb
    {
        get => GetFlag(BuildItemKeys.EmitPdb, defaultWhenMissing: true);
        set => SetFlag(BuildItemKeys.EmitPdb, value);
    }

    public bool EmitExecutionPlanText
    {
        get => GetFlag(BuildItemKeys.EmitExecutionPlanText, defaultWhenMissing: false);
        set => SetFlag(BuildItemKeys.EmitExecutionPlanText, value);
    }

    public Scope? PipelineScope
    {
        get => GetOptional<Scope>(BuildItemKeys.PipelineScope);
        set => SetOptional(BuildItemKeys.PipelineScope, value);
    }

    public IReadOnlyDictionary<string, ISchemaColumn[]>? PipelineInferredColumns
    {
        get => GetOptional<IReadOnlyDictionary<string, ISchemaColumn[]>>(BuildItemKeys.PipelineInferredColumns);
        set => SetOptional(BuildItemKeys.PipelineInferredColumns, value);
    }

    public IReadOnlyDictionary<string, IReadOnlySet<string>>? PipelineUsedColumns
    {
        get => GetOptional<IReadOnlyDictionary<string, IReadOnlySet<string>>>(BuildItemKeys.PipelineUsedColumns);
        set => SetOptional(BuildItemKeys.PipelineUsedColumns, value);
    }

    public bool StopAfterPlanning
    {
        get => GetFlag(BuildItemKeys.StopAfterPlanning, defaultWhenMissing: false);
        set => SetFlag(BuildItemKeys.StopAfterPlanning, value);
    }
}
