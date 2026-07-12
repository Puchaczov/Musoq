namespace Musoq.Converter.Build;

public partial class BuildItems
{
    internal ParseBuildArtifacts ParseArtifacts
    {
        get => new(RawQueryTree);
        set => RawQueryTree = value.RawQueryTree;
    }

    internal SemanticBuildArtifacts SemanticArtifacts
    {
        get => new()
        {
            TransformedQueryTree = TransformedQueryTree,
            UsedColumns = UsedColumns,
            UsedWhereNodes = UsedWhereNodes,
            SourcePlanRequestsPerSchema = SourcePlanRequestsPerSchema,
            SourceContractDiagnosticLocationsPerSchema = SourceContractDiagnosticLocationsPerSchema,
            ScriptParameterDefinitions = ScriptParameterDefinitions,
            ScriptVariableDefinitions = ScriptVariableDefinitions,
            SourceRuntimeSettingsBySourceContextId = SourceRuntimeSettingsBySourceContextId,
            SourceRuntimeSettingDescriptionsBySourceContextId = SourceRuntimeSettingDescriptionsBySourceContextId,
            HasDeclaredSourceRuntimeSettings = HasDeclaredSourceRuntimeSettings,
            HasSourceRuntimeSettingValues = HasSourceRuntimeSettingValues,
            PipelineScope = PipelineScope,
            PipelineInferredColumns = PipelineInferredColumns,
            PipelineUsedColumns = PipelineUsedColumns,
            CteExecutionPlan = CteExecutionPlan
        };
        set
        {
            TransformedQueryTree = value.TransformedQueryTree;
            UsedColumns = value.UsedColumns;
            UsedWhereNodes = value.UsedWhereNodes;
            SourcePlanRequestsPerSchema = value.SourcePlanRequestsPerSchema;
            SourceContractDiagnosticLocationsPerSchema = value.SourceContractDiagnosticLocationsPerSchema;
            ScriptParameterDefinitions = value.ScriptParameterDefinitions;
            ScriptVariableDefinitions = value.ScriptVariableDefinitions;
            SourceRuntimeSettingsBySourceContextId = value.SourceRuntimeSettingsBySourceContextId;
            SourceRuntimeSettingDescriptionsBySourceContextId = value.SourceRuntimeSettingDescriptionsBySourceContextId;
            HasDeclaredSourceRuntimeSettings = value.HasDeclaredSourceRuntimeSettings;
            HasSourceRuntimeSettingValues = value.HasSourceRuntimeSettingValues;
            PipelineScope = value.PipelineScope;
            PipelineInferredColumns = value.PipelineInferredColumns;
            PipelineUsedColumns = value.PipelineUsedColumns;
            CteExecutionPlan = value.CteExecutionPlan;
        }
    }

    internal PlanningBuildArtifacts PlanningArtifacts
    {
        get => new()
        {
            InitialLogicalPlan = InitialLogicalPlan,
            OptimizedLogicalPlan = OptimizedLogicalPlan,
            LogicalPlan = LogicalPlan,
            PlanningResult = PlanningResult,
            PlanningText = PlanningText,
            InitialPhysicalPlan = InitialPhysicalPlan,
            OptimizedPhysicalPlan = OptimizedPhysicalPlan,
            PhysicalPlan = PhysicalPlan
        };
        set
        {
            InitialLogicalPlan = value.InitialLogicalPlan;
            OptimizedLogicalPlan = value.OptimizedLogicalPlan;
            LogicalPlan = value.LogicalPlan;
            PlanningResult = value.PlanningResult;
            PlanningText = value.PlanningText;
            InitialPhysicalPlan = value.InitialPhysicalPlan;
            OptimizedPhysicalPlan = value.OptimizedPhysicalPlan;
            PhysicalPlan = value.PhysicalPlan;
        }
    }

    internal ExecutionBuildArtifacts ExecutionArtifacts
    {
        get => new()
        {
            ExecutionPlanBuildResult = ExecutionPlanBuildResult,
            InitialExecutionPlan = InitialExecutionPlan,
            OptimizedExecutionPlan = OptimizedExecutionPlan,
            ExecutionPlan = ExecutionPlan,
            ExecutionPlanText = ExecutionPlanText
        };
        set
        {
            ExecutionPlanBuildResult = value.ExecutionPlanBuildResult;
            InitialExecutionPlan = value.InitialExecutionPlan;
            OptimizedExecutionPlan = value.OptimizedExecutionPlan;
            ExecutionPlan = value.ExecutionPlan;
            ExecutionPlanText = value.ExecutionPlanText;
        }
    }

    internal RenderingBuildArtifacts RenderingArtifacts
    {
        get => new(RenderingArtifact)
        {
            QueryMethodRenderMetadata = QueryMethodRenderMetadata,
            CompatibilityReport = ExecutionTargetCompatibilityReport,
            RuntimeContract = TargetRuntimeContract,
            ReadinessReport = ExecutionTargetReadinessReport,
            SemanticsContract = ExecutionSemanticsContract
        };
        set
        {
            RenderingArtifact = value.Artifact;
            QueryMethodRenderMetadata = value.QueryMethodRenderMetadata;
            ExecutionTargetCompatibilityReport = value.CompatibilityReport;
            TargetRuntimeContract = value.RuntimeContract;
            ExecutionTargetReadinessReport = value.ReadinessReport;
            ExecutionSemanticsContract = value.SemanticsContract;
        }
    }

    internal CompilationBuildArtifacts CompilationArtifacts
    {
        get => FinalizationResult is { } finalizationResult
            ? new CompilationBuildArtifacts(finalizationResult)
            : new CompilationBuildArtifacts(EmitResult, ExecutableArtifact);
        set
        {
            FinalizationResult = value.FinalizationResult;
            if (value.TryGetEmitResult(out var emitResult))
                EmitResult = emitResult;

            ExecutableArtifact = value.Artifact;
            if (value.Artifact == null)
            {
                DllFile = value.DllFile;
                PdbFile = value.PdbFile;
            }
        }
    }
}
