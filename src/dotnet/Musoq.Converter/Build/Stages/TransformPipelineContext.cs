using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Converter.Build;

internal sealed record TransformPipelineContext
{
    public required string AssemblyName { get; init; }

    public required ISchemaProvider SchemaProvider { get; init; }

    public required CompilationOptions CompilationOptions { get; init; }

    public required DiagnosticContext DiagnosticContext { get; init; }

    public SchemaRegistry? SchemaRegistry { get; init; }

    public required bool EmitExecutionPlanText { get; init; }

    public required bool StopAfterPlanning { get; init; }

    public required bool EnableContextualExecution { get; init; }

    public required QueryResultMode QueryResultMode { get; init; }

    public required ExecutionTargetId ExecutionTarget { get; init; }

    public required QueryMethodRenderMetadata QueryMethodRenderMetadata { get; init; }

    public Type? OutputType { get; init; }

    public required IReadOnlyList<Type> AdditionalReferenceTypes { get; init; }

    public string? InterpreterSourceCode { get; init; }

    public string? OptimizerTraceText { get; init; }

    public Func<ISchemaProvider, IReadOnlyDictionary<string, string[]>, CompilationOptions, SchemaRegistry?, ILogger<BuildMetadataAndInferTypesVisitor>, BuildMetadataAndInferTypesVisitor>?
        CreateBuildMetadataAndInferTypesVisitor { get; init; }

    public static TransformPipelineContext From(BuildItems items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return new TransformPipelineContext
        {
            AssemblyName = items.AssemblyName,
            SchemaProvider = items.SchemaProvider,
            CompilationOptions = items.CompilationOptions,
            DiagnosticContext = items.DiagnosticContext,
            SchemaRegistry = items.SchemaRegistry,
            EmitExecutionPlanText = items.EmitExecutionPlanText,
            StopAfterPlanning = items.StopAfterPlanning,
            EnableContextualExecution = items.TryGetValue(BuildItemKeys.EnableContextualExecution, out var contextualExecution) && contextualExecution is true,
            QueryResultMode = items.QueryResultMode,
            ExecutionTarget = items.ExecutionTarget,
            QueryMethodRenderMetadata = items.QueryMethodRenderMetadata,
            OutputType = items.OutputType,
            AdditionalReferenceTypes = items.AdditionalReferenceTypes,
            InterpreterSourceCode = items.InterpreterSourceCode,
            OptimizerTraceText = items.OptimizerTraceText,
            CreateBuildMetadataAndInferTypesVisitor = items.CreateBuildMetadataAndInferTypesVisitor
        };
    }

    public TransformPipelineContext AppendTrace(OptimizationTrace trace)
    {
        return this with
        {
            OptimizerTraceText = OptimizationTraceTextPrinter.Append(OptimizerTraceText, trace)
        };
    }
}
