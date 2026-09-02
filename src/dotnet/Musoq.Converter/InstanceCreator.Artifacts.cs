using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Musoq.Converter.Build;
using Musoq.Converter.Diagnostics;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Targets.CSharpClr;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    public static ArtifactBuildResult CompileArtifactWithDiagnostics(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions = null)
    {
        var packageResult = CompileTargetPackageWithDiagnostics(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            ExecutionTargetIds.CSharpClr,
            compilationOptions);

        if (!packageResult.Succeeded)
            return ArtifactBuildResult.Failure(packageResult.Diagnostics, packageResult.CaughtException);

        CompiledQueryArtifact artifact;
        var diagnostics = packageResult.Diagnostics.ToList();
        try
        {
            artifact = CompiledQueryArtifactSupport.CreateCompiledArtifactFromPackage(
                packageResult.Package,
                CompiledQueryArtifactSupport.CurrentEngineVersion,
                CompiledQueryArtifact.CurrentArtifactFormatVersion,
                packageResult.CompilationOptionsSignature);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            diagnostics.Add(CreateArtifactDiagnostic($"Compiled artifact packaging failed: {ex.Message}"));
            return ArtifactBuildResult.Failure(diagnostics, ex);
        }

        return ArtifactBuildResult.Success(artifact, diagnostics);
    }

    internal static TargetPackageBuildResult CompileTargetPackageWithDiagnostics(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        ExecutionTargetId executionTarget,
        CompilationOptions? compilationOptions = null)
    {
        return CompileTargetPackageWithDiagnosticsCore(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            executionTarget,
            compilationOptions,
            configureItems: null);
    }

    internal static TargetPackageBuildResult CompileTargetPackageWithDiagnostics<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        ExecutionTargetId executionTarget,
        CompilationOptions? compilationOptions = null)
    {
        return CompileTargetPackageWithDiagnosticsCore(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            executionTarget,
            compilationOptions,
            items =>
            {
                items.QueryResultMode = QueryResultMode.TypedEnumerable;
                items.OutputType = typeof(TOut);
                items.AdditionalReferenceTypes = CreateTypedReferenceTypes<TOut>([]);
            });
    }

    private static TargetPackageBuildResult CompileTargetPackageWithDiagnosticsCore(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        ExecutionTargetId executionTarget,
        CompilationOptions? compilationOptions,
        Action<BuildItems>? configureItems)
    {
        ArgumentNullException.ThrowIfNull(schemaProvider);
        ArgumentNullException.ThrowIfNull(loggerResolver);

        if (string.IsNullOrWhiteSpace(executionTarget.Value))
            throw new ArgumentException("Execution target id cannot be empty.", nameof(executionTarget));

        if (string.IsNullOrWhiteSpace(script))
            return TargetPackageBuildResult.Failure(CreateEmptyQueryDiagnostics(script));

        var effectiveCompilationOptions = compilationOptions ?? new CompilationOptions();
        if (DiagnosticSqlCommandParser.TryParse(script, out var diagnosticCommand, out var parserDiagnostics))
        {
            if (parserDiagnostics is { Count: > 0 })
                return TargetPackageBuildResult.Failure(parserDiagnostics.ToArray());

            if (diagnosticCommand != null)
                return TargetPackageBuildResult.Failure(
                    [CreateArtifactDiagnostic("Diagnostic SQL commands cannot be compiled into reusable artifacts.")]);
        }

        var diagnosticContext = new DiagnosticContext(new SourceText(script));
        var items = CreateBuildItems(script, assemblyName, schemaProvider, diagnosticContext);
        items.ExecutionTarget = executionTarget;
        items.CompilationPurpose = CompilationPurpose.PortableArtifactPackaging;
        items.EmitPdb = Debugger.IsAttached;
        items.FinalizationPurpose = TargetFinalizationPurpose.PortableArtifactPackaging;
        items.CompilationOptions = effectiveCompilationOptions;
        ConfigureReusableArtifactRendering(items);
        configureItems?.Invoke(items);

        Exception? caughtException = null;
        try
        {
            Build(items, CreateExecutableBuildChain(loggerResolver));
            RejectUnsupportedMultiStatementQuery(items.RawQueryTree);
        }
        catch (CompilationException ce)
        {
            caughtException = ce;
            diagnosticContext.ReportException(ce);
        }
        catch (AstValidationException ave)
        {
            caughtException = ave;
            diagnosticContext.ReportException(ave);
        }
        catch (MultiStatementQueryException mse)
        {
            caughtException = mse;
            diagnosticContext.ReportException(mse);
        }
        catch (Exception ex) when (EvaluatorExceptionTaxonomy.IsExpectedQueryFailure(ex))
        {
            caughtException = ex;
            diagnosticContext.ReportException(ex);
        }

        var diagnostics = diagnosticContext.Diagnostics.ToList();
        if (diagnosticContext.HasErrors)
            return TargetPackageBuildResult.Failure(diagnostics, caughtException, items);

        if (items.ExecutableArtifact is not { } executableArtifact)
        {
            diagnostics.Add(CreateArtifactDiagnostic("Compilation succeeded but did not produce an executable artifact."));
            return TargetPackageBuildResult.Failure(diagnostics, buildItems: items);
        }

        var compilationOptionsSignature = CompiledQueryArtifactSupport.ComputeCompilationOptionsSignature(effectiveCompilationOptions);
        var renderingArtifacts = items.RenderingArtifacts;
        var renderingArtifact = renderingArtifacts.Artifact;
        var inspection = InspectRenderedArtifactForPackage(renderingArtifact, diagnostics, out caughtException);
        if (caughtException != null)
            return TargetPackageBuildResult.Failure(diagnostics, caughtException, items);

        if (!TryGetTargetAnalysisForPackage(
                renderingArtifacts,
                diagnostics,
                out var runtimeContract,
                out var readinessReport,
                out var semanticsContract))
        {
            return TargetPackageBuildResult.Failure(diagnostics, buildItems: items);
        }

        TargetArtifactPackage package;
        try
        {
            var semanticFacts = TargetArtifactSemanticFactsFactory.From(items);
            package = ExecutionTargetCatalog.CreateArtifactPackage(
                new TargetArtifactPackagingContext(
                    items.ExecutionTarget,
                    assemblyName,
                    script,
                    compilationOptionsSignature,
                    renderingArtifact,
                    executableArtifact,
                    semanticFacts,
                    semanticsContract,
                    runtimeContract,
                    readinessReport,
                    items.ExecutionPlan?.ExecutionIrVersion ?? TargetContractVersions.ExecutionIr));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            diagnostics.Add(CreateArtifactDiagnostic($"Compiled artifact packaging failed: {ex.Message}"));
            return TargetPackageBuildResult.Failure(diagnostics, ex, items);
        }

        return TargetPackageBuildResult.Success(
            package,
            inspection,
            diagnostics,
            items,
            compilationOptionsSignature);
    }

    private static void ConfigureReusableArtifactRendering(BuildItems items)
    {
        items.EnableContextualExecution = true;
    }

    private static bool TryGetTargetAnalysisForPackage(
        RenderingBuildArtifacts renderingArtifacts,
        ICollection<Diagnostic> diagnostics,
        out TargetRuntimeContract runtimeContract,
        out ExecutionTargetReadinessReport readinessReport,
        out ExecutionSemanticsContract semanticsContract)
    {
        runtimeContract = null!;
        readinessReport = null!;
        semanticsContract = null!;

        if (renderingArtifacts.CompatibilityReport == null ||
            renderingArtifacts.RuntimeContract == null ||
            renderingArtifacts.ReadinessReport == null ||
            renderingArtifacts.SemanticsContract == null)
        {
            diagnostics.Add(CreateArtifactDiagnostic(
                $"Compiled artifact packaging failed: target analysis artifacts are missing for execution target '{renderingArtifacts.Artifact.TargetId}'."));
            return false;
        }

        runtimeContract = renderingArtifacts.RuntimeContract;
        readinessReport = renderingArtifacts.ReadinessReport;
        semanticsContract = renderingArtifacts.SemanticsContract;
        return true;
    }

    public static BuildResult CreateExecutableFromArtifactWithDiagnostics(
        string script,
        ICompiledQueryArtifact artifact,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions = null,
        CompiledQueryArtifactTypeLoader? typeLoader = null)
    {
        return CreateExecutableFromArtifactWithDiagnosticsCore(
            script,
            artifact,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            loadOptions: null,
            typeLoader == null
                ? null
                : loadedArtifact => new CompiledQueryArtifactLoadResult(typeLoader(loadedArtifact)));
    }

    public static BuildResult CreateExecutableFromArtifactWithDiagnostics(
        string script,
        ICompiledQueryArtifact artifact,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompiledQueryArtifactLoadOptions? loadOptions,
        CompilationOptions? compilationOptions = null,
        CompiledQueryArtifactLoader? loader = null)
    {
        return CreateExecutableFromArtifactWithDiagnosticsCore(
            script,
            artifact,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            loadOptions,
            loader);
    }

    private static BuildResult CreateExecutableFromArtifactWithDiagnosticsCore(
        string script,
        ICompiledQueryArtifact artifact,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions,
        CompiledQueryArtifactLoadOptions? loadOptions,
        CompiledQueryArtifactLoader? loader)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(schemaProvider);
        ArgumentNullException.ThrowIfNull(loggerResolver);

        if (string.IsNullOrWhiteSpace(script))
            return BuildResult.Failure(CreateEmptyQueryDiagnostics(script), script ?? string.Empty);

        var artifactDiagnostics = new List<Diagnostic>();
        if (!CSharpClrCompiledArtifactLoader.TryGetRequiredMetadata(
                artifact,
                CompiledQueryArtifactSupport.MetadataAssemblyName,
                artifactDiagnostics,
                out var assemblyName))
        {
            return BuildResult.Failure(artifactDiagnostics, script);
        }

        var effectiveLoadOptions = loadOptions ?? CompiledQueryArtifactLoadOptions.Default;
        if (!TryValidateLoadOptions(effectiveLoadOptions, artifactDiagnostics))
            return BuildResult.Failure(artifactDiagnostics, script);

        var effectiveCompilationOptions = compilationOptions ?? new CompilationOptions();
        if (DiagnosticSqlCommandParser.TryParse(script, out var diagnosticCommand, out var parserDiagnostics))
        {
            if (parserDiagnostics is { Count: > 0 })
                return BuildResult.Failure(parserDiagnostics.ToArray(), script);

            if (diagnosticCommand != null)
                return BuildResult.Failure(
                    [CreateArtifactDiagnostic("Diagnostic SQL commands cannot be loaded from reusable artifacts.")],
                    script);
        }

        var diagnosticContext = new DiagnosticContext(new SourceText(script));
        var items = CreateBuildItems(script, assemblyName, schemaProvider, diagnosticContext);
        items.CompilationPurpose = CompilationPurpose.ArtifactValidation;
        items.EmitPdb = false;
        items.CompilationOptions = effectiveCompilationOptions;
        ConfigureReusableArtifactRendering(items);
        items.StopAfterPlanning =
            effectiveLoadOptions.ValidationMode == CompiledQueryArtifactValidationMode.Fast;

        Exception? caughtException = null;
        try
        {
            Build(items, CreateInspectionBuildChain(loggerResolver));
            RejectUnsupportedMultiStatementQuery(items.RawQueryTree);
        }
        catch (CompilationException ce)
        {
            caughtException = ce;
            diagnosticContext.ReportException(ce);
        }
        catch (AstValidationException ave)
        {
            caughtException = ave;
            diagnosticContext.ReportException(ave);
        }
        catch (MultiStatementQueryException mse)
        {
            caughtException = mse;
            diagnosticContext.ReportException(mse);
        }
        catch (Exception ex) when (EvaluatorExceptionTaxonomy.IsExpectedQueryFailure(ex))
        {
            caughtException = ex;
            diagnosticContext.ReportException(ex);
        }

        var diagnostics = diagnosticContext.Diagnostics.ToList();
        if (diagnosticContext.HasErrors)
            return BuildResult.Failure(diagnostics, script, caughtException, items);

        CSharpClrCompiledArtifactLoader.ValidateArtifactCompatibility(
            script,
            artifact,
            assemblyName,
            effectiveCompilationOptions,
            effectiveLoadOptions.ValidationMode,
            items,
            diagnostics);
        if (diagnostics.Any(static diagnostic => diagnostic.IsError))
            return BuildResult.Failure(diagnostics, script, buildItems: items);

        ClrLoadedExecutableArtifact loadedExecutable;
        try
        {
            loadedExecutable = loader != null
                ? CSharpClrCompiledArtifactLoader.CreateLoadedExecutableArtifact(loader(artifact))
                : CSharpClrCompiledArtifactLoader.LoadExecutableArtifactFromArtifactBytes(artifact);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            diagnostics.Add(CreateArtifactDiagnostic($"Compiled artifact type loading failed: {ex.Message}"));
            return BuildResult.Failure(diagnostics, script, ex, items);
        }

        CSharpClrCompiledArtifactLoader.ValidateLoadedRunnableType(artifact, loadedExecutable.RunnableType, diagnostics);
        if (diagnostics.Any(static diagnostic => diagnostic.IsError))
        {
            CSharpClrCompiledArtifactLoader.DisposeArtifactLifetime(loadedExecutable.LifetimeOwner);
            return BuildResult.Failure(diagnostics, script, buildItems: items);
        }

        ITableRunnable runnable;
        try
        {
            var activator = ExecutionTargetCatalog.ResolveActivator(loadedExecutable.TargetId);
            runnable = activator.ActivateTable(
                loadedExecutable,
                    new QueryRuntimeBinding(
                    items.SchemaProvider,
                    items.SourceRuntimeSettingsBySourceContextId,
                    items.SourceRuntimeSettingDescriptionsBySourceContextId,
                    CreateSourceExecutionPlans(items)));
            runnable.Logger = loggerResolver.ResolveLogger();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            CSharpClrCompiledArtifactLoader.DisposeArtifactLifetime(loadedExecutable.LifetimeOwner);
            diagnostics.Add(CreateArtifactDiagnostic($"Compiled artifact runnable creation failed: {ex.Message}"));
            return BuildResult.Failure(diagnostics, script, ex, items);
        }

        return BuildResult.Success(new CompiledQuery(runnable, loadedExecutable.LifetimeOwner), diagnostics, script, items);
    }

    private static bool TryValidateLoadOptions(
        CompiledQueryArtifactLoadOptions loadOptions,
        ICollection<Diagnostic> diagnostics)
    {
        if (loadOptions.ValidationMode is CompiledQueryArtifactValidationMode.Fast or
            CompiledQueryArtifactValidationMode.StrictGeneratedCodeHash)
        {
            return true;
        }

        diagnostics.Add(CreateArtifactDiagnostic(
            $"Compiled artifact validation mode '{loadOptions.ValidationMode}' is not supported."));
        return false;
    }

    private static RenderedQueryInspection? InspectRenderedArtifactForPackage(
        RenderedQueryArtifact renderedArtifact,
        ICollection<Diagnostic> diagnostics,
        out Exception? caughtException)
    {
        caughtException = null;
        try
        {
            return ExecutionTargetCatalog.TryInspectArtifact(renderedArtifact, out var inspection)
                ? inspection
                : null;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            caughtException = ex;
            diagnostics.Add(CreateArtifactDiagnostic($"Compiled artifact inspection failed: {ex.Message}"));
            return null;
        }
    }

    private static IReadOnlyList<Diagnostic> CreateEmptyQueryDiagnostics(string? script)
    {
        var emptySourceText = new SourceText(script ?? string.Empty);
        var emptyContext = new DiagnosticContext(emptySourceText);
        emptyContext.ReportError(
            DiagnosticCode.MQ2016_IncompleteStatement,
            "The query is empty. Provide a valid SQL query starting with SELECT, WITH, DESC, TABLE, COUPLE, or an optional param(...) block.",
            TextSpan.Empty);
        return emptyContext.Diagnostics.ToList();
    }

    private static Diagnostic CreateArtifactDiagnostic(string message)
    {
        return Diagnostic.ErrorUnknownLocation(
            DiagnosticCode.MQ8002_CompiledArtifactIncompatible,
            message,
            sourceKind: DiagnosticSourceKind.GeneratedSource);
    }
}
