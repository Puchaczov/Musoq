using System;
using System.Collections.Generic;

namespace Musoq.Converter.Build;

/// <summary>
/// Central registry of the string keys used to store build artifacts in <see cref="BuildItems"/>.
/// Build artifacts must be accessed through typed <see cref="BuildItems"/> members; new keys belong
/// here rather than as inline string literals scattered across accessors.
/// </summary>
internal static class BuildItemKeys
{
    public const string DllFile = "DLL_FILE";
    public const string PdbFile = "PDB_FILE";
    public const string ExecutableArtifact = "EXECUTABLE_ARTIFACT";
    public const string TransformedQueryTree = "TRANSFORMED_QUERY_TREE";
    public const string RawQueryTree = "RAW_QUERY_TREE";
    public const string RawQuery = "RAW_QUERY";
    public const string AssemblyName = "ASSEMBLY_NAME";
    public const string SchemaProvider = "SCHEMA_PROVIDER";
    public const string SourceRuntimeSettingsBySourceContextId = "SOURCE_RUNTIME_SETTINGS_BY_SOURCE_CONTEXT_ID";
    public const string SourceRuntimeSettingDescriptionsBySourceContextId = "SOURCE_RUNTIME_SETTING_DESCRIPTIONS_BY_SOURCE_CONTEXT_ID";
    public const string HasDeclaredSourceRuntimeSettings = "HAS_DECLARED_SOURCE_RUNTIME_SETTINGS";
    public const string HasSourceRuntimeSettingValues = "HAS_SOURCE_RUNTIME_SETTING_VALUES";
    public const string RenderingArtifact = "RENDERING_ARTIFACT";
    public const string Compilation = "COMPILATION";
    public const string AccessToClassPath = "ACCESS_TO_CLASS_PATH";
    public const string EmitResult = "EMIT_RESULT";
    public const string FinalizationResult = "FINALIZATION_RESULT";
    public const string UsedColumns = "USED_COLUMNS";
    public const string UsedWhereNodes = "USED_WHERE_NODES";
    public const string SourcePlanRequestsPerSchema = "SOURCE_PLAN_REQUESTS_PER_SCHEMA";
    public const string SourceContractDiagnosticLocationsPerSchema = "SOURCE_CONTRACT_DIAGNOSTIC_LOCATIONS_PER_SCHEMA";
    public const string ScriptParameterDefinitions = "SCRIPT_PARAMETER_DEFINITIONS";
    public const string CreateBuildMetadataAndInferTypesVisitor = "CREATE_BUILD_METADATA_AND_INFER_TYPES_VISITOR";
    public const string CompilationOptions = "COMPILATION_OPTIONS";
    public const string QueryResultMode = "QUERY_RESULT_MODE";
    public const string ExecutionTarget = "EXECUTION_TARGET";
    public const string QueryMethodRenderMetadata = "QUERY_METHOD_RENDER_METADATA";
    public const string ExecutionTargetCompatibilityReport = "EXECUTION_TARGET_COMPATIBILITY_REPORT";
    public const string TargetRuntimeContract = "TARGET_RUNTIME_CONTRACT";
    public const string ExecutionTargetReadinessReport = "EXECUTION_TARGET_READINESS_REPORT";
    public const string ExecutionSemanticsContract = "EXECUTION_SEMANTICS_CONTRACT";
    public const string OutputType = "OUTPUT_TYPE";
    public const string AdditionalReferenceTypes = "ADDITIONAL_REFERENCE_TYPES";
    public const string SchemaRegistry = "SCHEMA_REGISTRY";
    public const string InterpreterSourceCode = "INTERPRETER_SOURCE_CODE";
    public const string CteExecutionPlan = "CTE_EXECUTION_PLAN";
    public const string LogicalPlan = "LOGICAL_PLAN";
    public const string PhysicalPlan = "PHYSICAL_PLAN";
    public const string PlanningResult = "PLANNING_RESULT";
    public const string PlanningText = "PLANNING_TEXT";
    public const string ExecutionPlan = "EXECUTION_PLAN";
    public const string ExecutionPlanBuildResult = "EXECUTION_PLAN_BUILD_RESULT";
    public const string ExecutionPlanText = "EXECUTION_PLAN_TEXT";
    public const string SourceText = "SOURCE_TEXT";
    public const string DiagnosticContext = "DIAGNOSTIC_CONTEXT";
    public const string EmitPdb = "EMIT_PDB";
    public const string EmitExecutionPlanText = "EMIT_EXECUTION_PLAN_TEXT";
    public const string PipelineScope = "PIPELINE_SCOPE";
    public const string PipelineInferredColumns = "PIPELINE_INFERRED_COLUMNS";
    public const string PipelineUsedColumns = "PIPELINE_USED_COLUMNS";
    public const string StopAfterPlanning = "STOP_AFTER_PLANNING";

    public const string InitialLogicalPlan = "INITIAL_LOGICAL_PLAN";
    public const string OptimizedLogicalPlan = "OPTIMIZED_LOGICAL_PLAN";
    public const string InitialPhysicalPlan = "INITIAL_PHYSICAL_PLAN";
    public const string OptimizedPhysicalPlan = "OPTIMIZED_PHYSICAL_PLAN";
    public const string InitialExecutionPlan = "INITIAL_EXECUTION_PLAN";
    public const string OptimizedExecutionPlan = "OPTIMIZED_EXECUTION_PLAN";
    public const string OptimizerTraceText = "OPTIMIZER_TRACE_TEXT";

    public const string ScriptVariableDefinitions = "SCRIPT_VARIABLE_DEFINITIONS";
}
