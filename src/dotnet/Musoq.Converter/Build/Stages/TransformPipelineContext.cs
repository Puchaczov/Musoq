using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Converter.Build;

internal sealed record TransformPipelineContext
{
    public required string AssemblyName { get; init; }

    public required ISchemaProvider SchemaProvider { get; init; }

    public required CompilationOptions CompilationOptions { get; init; }

    public required DiagnosticContext DiagnosticContext { get; init; }

    public SourceText? SourceText { get; init; }

    public SchemaRegistry? SchemaRegistry { get; init; }

    public required bool EmitExecutionPlanText { get; init; }

    public required bool StopAfterPlanning { get; init; }

    public required bool EnableContextualExecution { get; init; }

    public required QueryResultMode QueryResultMode { get; init; }

    public required ExecutionTargetId ExecutionTarget { get; init; }

    public required CompilationPurpose CompilationPurpose { get; init; }
    public required bool EmitPdb { get; init; }
    public required QueryMethodRenderMetadata QueryMethodRenderMetadata { get; init; }

    public Type? OutputType { get; init; }

    public required IReadOnlyList<Type> AdditionalReferenceTypes { get; init; }

    public string? InterpreterSourceCode { get; init; }

    public IReadOnlyList<OptimizationTrace> OptimizerTraces { get; init; } = [];

    public string? OptimizerTraceText => EmitExecutionPlanText
        ? OptimizationTraceTextPrinter.Print(OptimizerTraces.ToArray())
        : null;

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
            SourceText = items.SourceText,
            SchemaRegistry = items.SchemaRegistry,
            EmitExecutionPlanText = items.EmitExecutionPlanText,
            StopAfterPlanning = items.StopAfterPlanning,
            EnableContextualExecution = items.EnableContextualExecution,
            QueryResultMode = items.QueryResultMode,
            ExecutionTarget = items.ExecutionTarget,
            CompilationPurpose = items.CompilationPurpose,
            EmitPdb = items.EmitPdb,
            QueryMethodRenderMetadata = items.QueryMethodRenderMetadata,
            OutputType = items.OutputType,
            AdditionalReferenceTypes = items.AdditionalReferenceTypes,
            InterpreterSourceCode = items.InterpreterSourceCode,
            OptimizerTraces = [],
            CreateBuildMetadataAndInferTypesVisitor = items.CreateBuildMetadataAndInferTypesVisitor
        };
    }

    public TransformPipelineContext AppendTrace(OptimizationTrace trace)
    {
        if (!EmitExecutionPlanText || trace == null)
            return this;

        var traces = new OptimizationTrace[OptimizerTraces.Count + 1];
        for (var index = 0; index < OptimizerTraces.Count; index++)
            traces[index] = OptimizerTraces[index];
        traces[^1] = trace;

        return this with
        {
            OptimizerTraces = traces
        };
    }
}
