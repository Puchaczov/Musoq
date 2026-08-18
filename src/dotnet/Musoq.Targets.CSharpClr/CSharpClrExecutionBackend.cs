using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Runtime;

namespace Musoq.Targets.CSharpClr;

internal sealed class CSharpClrExecutionBackend : IQueryExecutionBackend
{
    private readonly EvaluatorRuntimeEnvironment _runtimeEnvironment;

    internal CSharpClrExecutionBackend()
        : this(new EvaluatorRuntimeEnvironment())
    {
    }

    internal CSharpClrExecutionBackend(EvaluatorRuntimeEnvironment runtimeEnvironment)
    {
        _runtimeEnvironment = runtimeEnvironment ?? throw new ArgumentNullException(nameof(runtimeEnvironment));
    }

    public ExecutionTargetId TargetId => ExecutionTargetIds.CSharpClr;

    public ExecutionTargetCapabilities Capabilities { get; } = ExecutionTargetCapabilities.CSharpClr;

    public TargetRenderResult Render(TargetRenderRequest request)
    {
        var inputs = RequireInputs(request);
        var assemblyName = inputs.AssemblyName;
        var safeNamespaceName = inputs.NamespaceName;
        var generator = _runtimeEnvironment.Generator;

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
                ForceTableResultMaterialization: inputs.CompilationOptions.ForceTableResultMaterialization,
                EnableContextualExecution: inputs.EnableContextualExecution));

        var renderer = new CSharpRenderer(renderContext, inputs.ExecutionBindings);
        const string queryIdentifier = "compiled";
        var executionPlan = request.ExecutionPlan;
        ExecutionQueryRenderOutcome renderOutcome;
        using (TargetRenderTelemetry.BeginPhase("render.execution-method"))
            renderOutcome = renderer.TryRenderExecutionQueryMethod(executionPlan, queryIdentifier);
        if (renderOutcome.Method is not { } executionQueryResult)
        {
            var reason = string.IsNullOrWhiteSpace(renderOutcome.UnsupportedReason)
                ? "Execution IR C# backend did not produce a query method."
                : renderOutcome.UnsupportedReason;
            return TargetRenderResult.Failed(
                TargetId,
                [TargetDiagnostic.Error(TargetDiagnosticCodes.UnsupportedLowering, reason)]);
        }

        CompilationUnitSyntax compilationUnit;
        using (TargetRenderTelemetry.BeginPhase("render.class-assembly"))
        {
            renderContext.AddClassMember(executionQueryResult.MethodDeclaration);
            compilationUnit = renderer.RenderCompilationUnit(
                queryIdentifier,
                ExecutionPlanInventory.CountTableSlots(executionPlan),
                ExecutionPlanInventory.CountCteIndexSlots(executionPlan));
        }
        var readabilityResult = new CodegenReadabilityOptimizer().Optimize(
            compilationUnit,
            inputs.RenderProfile);
        compilationUnit = readabilityResult.OptimizedCode;

        var compilationContext = new CompilationContextManager(
            _runtimeEnvironment.CreateCompilation(assemblyName),
            _runtimeEnvironment);
        compilationContext.InitializeDefaults();
        try
        {
            var referenceAssemblies = CSharpClrReferenceAssemblyCollector.Collect(
                request.CompatibilityReport,
                inputs.ExecutionBindings,
                inputs.ReferenceAssemblies,
                inputs.AdditionalReferenceTypes,
                inputs.OutputType,
                _runtimeEnvironment.PreloadedAssemblyPaths);

            compilationContext.InitializeCoreReferences(referenceAssemblies);
        }
        catch (CSharpClrReferenceDiscoveryException exception)
        {
            return CreateMissingReferenceResult(exception);
        }
        catch (Exception exception) when (IsExpectedMetadataReferenceFailure(exception))
        {
            return CreateMissingReferenceResult(
                new CSharpClrReferenceDiscoveryException(
                    "<unknown>",
                    "execution-plan CLR reference",
                    CSharpClrReferenceDiscoveryException.GetStableReason(exception),
                    exception));
        }

        var generatedSourcePath = $"{safeNamespaceName}.g.cs";
        compilationContext.AddSyntaxTree(ClassEmitter.CreateSyntaxTreeDirect(
            compilationUnit,
            inputs.RenderProfile,
            generatedSourcePath));
        if (!string.IsNullOrEmpty(inputs.InterpreterSourceCode))
        {
            compilationContext.TrackNamespace("Musoq.Generated.Interpreters");
            compilationContext.AddSyntaxTree(CSharpSyntaxTree.ParseText(
                inputs.InterpreterSourceCode,
                new CSharpParseOptions(LanguageVersion.CSharp13),
                $"{safeNamespaceName}.interpreter.g.cs",
                Encoding.UTF8));
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

    internal static TargetRenderResult CreateMissingReferenceResult(
        CSharpClrReferenceDiscoveryException exception)
    {
        return TargetRenderResult.Failed(
            ExecutionTargetIds.CSharpClr,
            [TargetDiagnostic.Error(
                TargetDiagnosticCodes.MissingClrReference,
                exception.Message)]);
    }

    private static bool IsExpectedMetadataReferenceFailure(Exception exception) =>
        exception is FileNotFoundException or
            BadImageFormatException or
            FileLoadException or
            IOException;

}
