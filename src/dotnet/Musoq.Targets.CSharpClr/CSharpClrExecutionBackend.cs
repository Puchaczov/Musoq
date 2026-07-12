using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.IR.Execution;
using Musoq.Targets.CSharpClr.Optimization.Codegen;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

internal sealed class CSharpClrExecutionBackend : IQueryExecutionBackend
{
    public ExecutionTargetId TargetId => ExecutionTargetIds.CSharpClr;

    public ExecutionTargetCapabilities Capabilities { get; } = ExecutionTargetCapabilities.CSharpClr;

    public TargetRenderResult Render(TargetRenderRequest request)
    {
        var inputs = RequireInputs(request);
        var assemblyName = inputs.AssemblyName;
        var safeNamespaceName = inputs.NamespaceName;
        var generator = RoslynSharedFactory.Generator;

        var renderContext = new RenderContext(
            generator,
            new RenderContextOptions(
                Scope: inputs.Scope,
                AssemblyName: safeNamespaceName,
                ScriptParameterDefinitions: inputs.ScriptParameterDefinitions,
                ScriptVariableDefinitions: inputs.ScriptVariableDefinitions,
                InstrumentationMode: inputs.CompilationOptions.InstrumentationMode,
                ResultMode: inputs.QueryResultMode,
                FinalResultSinkKind: ResolveFinalResultSinkKind(inputs.QueryResultMode),
                OutputType: inputs.OutputType,
                ForceTableResultMaterialization: inputs.CompilationOptions.ForceTableResultMaterialization));

        var renderer = new CSharpRenderer(renderContext);
        const string queryIdentifier = "compiled";
        var executionPlan = request.ExecutionPlan;
        var renderOutcome = renderer.TryRenderExecutionQueryMethod(executionPlan, queryIdentifier);
        if (renderOutcome.Method is not { } executionQueryResult)
        {
            var reason = string.IsNullOrWhiteSpace(renderOutcome.UnsupportedReason)
                ? "Execution IR C# backend did not produce a query method."
                : renderOutcome.UnsupportedReason;
            return TargetRenderResult.Failed(
                TargetId,
                [TargetDiagnostic.Error(TargetDiagnosticCodes.UnsupportedLowering, reason)]);
        }

        renderContext.AddClassMember(executionQueryResult.MethodDeclaration);
        var compilationUnit = renderer.RenderCompilationUnit(
            queryIdentifier,
            ExecutionPlanInventory.CountTableSlots(executionPlan),
            ExecutionPlanInventory.CountCteIndexSlots(executionPlan));
        var readabilityResult = new CodegenReadabilityOptimizer().Optimize(compilationUnit);
        compilationUnit = readabilityResult.OptimizedCode;

        var compilationContext = new CompilationContextManager(
            RoslynSharedFactory.CreateCompilation(assemblyName));
        compilationContext.InitializeDefaults();
        var referenceAssemblies = inputs.ReferenceAssemblies.ToList();
        foreach (var referenceType in inputs.AdditionalReferenceTypes)
        {
            if (!referenceAssemblies.Contains(referenceType.Assembly))
                referenceAssemblies.Add(referenceType.Assembly);
        }

        if (inputs.OutputType?.Assembly is { } outputAssembly && !referenceAssemblies.Contains(outputAssembly))
            referenceAssemblies.Add(outputAssembly);

        compilationContext.InitializeCoreReferences(referenceAssemblies);
        compilationContext.AddSyntaxTree(ClassEmitter.CreateSyntaxTreeDirect(compilationUnit));
        if (!string.IsNullOrEmpty(inputs.InterpreterSourceCode))
        {
            compilationContext.TrackNamespace("Musoq.Generated.Interpreters");
            compilationContext.AddSyntaxTree(CSharpSyntaxTree.ParseText(
                inputs.InterpreterSourceCode,
                new CSharpParseOptions(LanguageVersion.CSharp11)));
        }

        var artifact = new CSharpRenderedQueryArtifact(
            compilationContext.GetCompilation(),
            $"{safeNamespaceName}.CompiledQuery",
            executionQueryResult.Metadata,
            readabilityResult.Trace);
        return TargetRenderResult.Succeeded(artifact);
    }

    private static CSharpClrRenderInputs RequireInputs(TargetRenderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.BackendInputs is CSharpClrRenderInputs inputs)
            return inputs;

        var inputTypeName = request.BackendInputs?.GetType().Name ?? "<null>";
        throw CreateUnsupportedExecutionIrException(
            $"CSharpClr backend requires {nameof(CSharpClrRenderInputs)}, but received {inputTypeName}.");
    }

    private static FinalResultSinkKind ResolveFinalResultSinkKind(QueryResultMode resultMode)
    {
        return resultMode switch
        {
            QueryResultMode.TypedEnumerable => FinalResultSinkKind.TypedSerialEnumerable,
            QueryResultMode.TableViaRows => FinalResultSinkKind.TableRowsMaterialized,
            _ => FinalResultSinkKind.TableDirect
        };
    }

    private static NotSupportedException CreateUnsupportedExecutionIrException(string? unsupportedReason)
    {
        var reason = string.IsNullOrWhiteSpace(unsupportedReason)
            ? "Execution IR lowering did not produce a plan."
            : unsupportedReason;

        return new NotSupportedException(
            $"Execution IR does not support this query shape and old physical rendering is disabled: {reason}");
    }
}
