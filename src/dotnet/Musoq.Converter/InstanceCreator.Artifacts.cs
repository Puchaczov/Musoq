using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        return CompileArtifactWithDiagnostics(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            CancellationToken.None);
    }

    public static ArtifactBuildResult CompileArtifactWithDiagnostics(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CancellationToken cancellationToken)
    {
        return CompileArtifactWithDiagnostics(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            null,
            cancellationToken);
    }

    public static ArtifactBuildResult CompileArtifactWithDiagnostics(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var packageResult = CompileTargetPackageWithDiagnostics(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            ExecutionTargetIds.CSharpClr,
            compilationOptions,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
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
                packageResult.CompilationOptionsSignature,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            diagnostics.Add(CreateArtifactDiagnostic($"Compiled artifact packaging failed: {ex.Message}"));
            return ArtifactBuildResult.Failure(diagnostics, ex);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return ArtifactBuildResult.Success(artifact, diagnostics);
    }

    internal static TargetPackageBuildResult CompileTargetPackageWithDiagnostics(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        ExecutionTargetId executionTarget,
        CompilationOptions? compilationOptions = null,
        CancellationToken cancellationToken = default)
    {
        return CompileTargetPackageWithDiagnosticsCore(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            executionTarget,
            compilationOptions,
            configureItems: null,
            cancellationToken);
    }

    internal static TargetPackageBuildResult CompileTargetPackageWithDiagnostics<TOut>(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        ExecutionTargetId executionTarget,
        CompilationOptions? compilationOptions = null,
        CancellationToken cancellationToken = default)
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
            },
            cancellationToken);
    }

    private static TargetPackageBuildResult CompileTargetPackageWithDiagnosticsCore(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        ExecutionTargetId executionTarget,
        CompilationOptions? compilationOptions,
        Action<BuildItems>? configureItems,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schemaProvider);
        ArgumentNullException.ThrowIfNull(loggerResolver);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(executionTarget.Value))
            throw new ArgumentException("Execution target id cannot be empty.", nameof(executionTarget));

        if (string.IsNullOrWhiteSpace(script))
            return TargetPackageBuildResult.Failure(CreateEmptyQueryDiagnostics(script));

        var effectiveCompilationOptions = compilationOptions ?? new CompilationOptions();
        if (DiagnosticSqlCommandParser.TryParse(script, out var diagnosticCommand, out var parserDiagnostics))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (parserDiagnostics is { Count: > 0 })
                return TargetPackageBuildResult.Failure(parserDiagnostics.ToArray());

            if (diagnosticCommand != null)
                return TargetPackageBuildResult.Failure(
                    [CreateArtifactDiagnostic("Diagnostic SQL commands cannot be compiled into reusable artifacts.")]);
        }

        var diagnosticContext = new DiagnosticContext(new SourceText(script));
        var items = CreateBuildItems(script, assemblyName, schemaProvider, diagnosticContext, cancellationToken);
        items.ExecutionTarget = executionTarget;
        items.CompilationPurpose = CompilationPurpose.PortableArtifactPackaging;
        items.EmitPdb = Debugger.IsAttached;
        items.FinalizationPurpose = TargetFinalizationPurpose.PortableArtifactPackaging;
        items.CompilationOptions = effectiveCompilationOptions;
        ConfigureReusableArtifactRendering(items);
        configureItems?.Invoke(items);
        cancellationToken.ThrowIfCancellationRequested();

        Exception? caughtException = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Build(items, CreateExecutableBuildChain(loggerResolver));
            RejectUnsupportedMultiStatementQuery(items.RawQueryTree);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (CompilationException ce)
        {
            cancellationToken.ThrowIfCancellationRequested();
            caughtException = ce;
            diagnosticContext.ReportException(ce);
        }
        catch (AstValidationException ave)
        {
            cancellationToken.ThrowIfCancellationRequested();
            caughtException = ave;
            diagnosticContext.ReportException(ave);
        }
        catch (MultiStatementQueryException mse)
        {
            cancellationToken.ThrowIfCancellationRequested();
            caughtException = mse;
            diagnosticContext.ReportException(mse);
        }
        catch (Exception ex) when (EvaluatorExceptionTaxonomy.IsExpectedQueryFailure(ex))
        {
            cancellationToken.ThrowIfCancellationRequested();
            caughtException = ex;
            diagnosticContext.ReportException(ex);
        }

        var diagnostics = diagnosticContext.Diagnostics.ToList();
        cancellationToken.ThrowIfCancellationRequested();
        if (diagnosticContext.HasErrors)
            return TargetPackageBuildResult.Failure(diagnostics, caughtException, items);

        cancellationToken.ThrowIfCancellationRequested();
        if (items.ExecutableArtifact is not { } executableArtifact)
        {
            diagnostics.Add(CreateArtifactDiagnostic("Compilation succeeded but did not produce an executable artifact."));
            return TargetPackageBuildResult.Failure(diagnostics, buildItems: items);
        }

        var compilationOptionsSignature = CompiledQueryArtifactSupport.ComputeCompilationOptionsSignature(effectiveCompilationOptions);
        cancellationToken.ThrowIfCancellationRequested();
        var renderingArtifacts = items.RenderingArtifacts;
        var renderingArtifact = renderingArtifacts.Artifact;
        var inspection = InspectRenderedArtifactForPackage(
            renderingArtifact,
            diagnostics,
            cancellationToken,
            out caughtException);
        cancellationToken.ThrowIfCancellationRequested();
        if (caughtException != null)
            return TargetPackageBuildResult.Failure(diagnostics, caughtException, items);

        cancellationToken.ThrowIfCancellationRequested();
        if (!TryGetTargetAnalysisForPackage(
                renderingArtifacts,
                diagnostics,
                out var runtimeContract,
                out var readinessReport,
                out var semanticsContract))
        {
            return TargetPackageBuildResult.Failure(diagnostics, buildItems: items);
        }

        cancellationToken.ThrowIfCancellationRequested();
        TargetArtifactPackage package;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var semanticFacts = TargetArtifactSemanticFactsFactory.From(items, cancellationToken);
            package = ExecutionTargetCatalog.CreateArtifactPackage(
                new TargetArtifactPackagingContext(
                    items.CancellationToken,
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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            diagnostics.Add(CreateArtifactDiagnostic($"Compiled artifact packaging failed: {ex.Message}"));
            return TargetPackageBuildResult.Failure(diagnostics, ex, items);
        }

        cancellationToken.ThrowIfCancellationRequested();
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
        return CreateExecutableFromArtifactWithDiagnostics(
            script,
            artifact,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            typeLoader,
            CancellationToken.None);
    }

    public static BuildResult CreateExecutableFromArtifactWithDiagnostics(
        string script,
        ICompiledQueryArtifact artifact,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CancellationToken cancellationToken)
    {
        return CreateExecutableFromArtifactWithDiagnosticsCore(
            script,
            artifact,
            schemaProvider,
            loggerResolver,
            compilationOptions: null,
            loadOptions: null,
            loader: null,
            cancellationToken);
    }

    public static BuildResult CreateExecutableFromArtifactWithDiagnostics(
        string script,
        ICompiledQueryArtifact artifact,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions,
        CancellationToken cancellationToken)
    {
        return CreateExecutableFromArtifactWithDiagnostics(
            script,
            artifact,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            null,
            cancellationToken);
    }

    public static BuildResult CreateExecutableFromArtifactWithDiagnostics(
        string script,
        ICompiledQueryArtifact artifact,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions,
        CompiledQueryArtifactTypeLoader? typeLoader,
        CancellationToken cancellationToken)
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
                : loadedArtifact => new CompiledQueryArtifactLoadResult(typeLoader(loadedArtifact)),
            cancellationToken);
    }

    public static BuildResult CreateExecutableFromArtifactWithDiagnostics(
        string script,
        ICompiledQueryArtifact artifact,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompiledQueryArtifactTypeLoaderWithCancellation? typeLoader,
        CancellationToken cancellationToken)
    {
        return CreateExecutableFromArtifactWithDiagnosticsCore(
            script,
            artifact,
            schemaProvider,
            loggerResolver,
            compilationOptions: null,
            loadOptions: null,
            typeLoader == null
                ? null
                : loadedArtifact => new CompiledQueryArtifactLoadResult(typeLoader(loadedArtifact, cancellationToken)),
            cancellationToken);
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
        return CreateExecutableFromArtifactWithDiagnostics(
            script,
            artifact,
            schemaProvider,
            loggerResolver,
            loadOptions,
            compilationOptions,
            loader,
            CancellationToken.None);
    }

    public static BuildResult CreateExecutableFromArtifactWithDiagnostics(
        string script,
        ICompiledQueryArtifact artifact,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompiledQueryArtifactLoadOptions? loadOptions,
        CancellationToken cancellationToken)
    {
        return CreateExecutableFromArtifactWithDiagnosticsCore(
            script,
            artifact,
            schemaProvider,
            loggerResolver,
            compilationOptions: null,
            loadOptions,
            loader: null,
            cancellationToken);
    }

    public static BuildResult CreateExecutableFromArtifactWithDiagnostics(
        string script,
        ICompiledQueryArtifact artifact,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompiledQueryArtifactLoadOptions? loadOptions,
        CompilationOptions? compilationOptions,
        CancellationToken cancellationToken)
    {
        return CreateExecutableFromArtifactWithDiagnosticsCore(
            script,
            artifact,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            loadOptions,
            loader: null,
            cancellationToken);
    }

    public static BuildResult CreateExecutableFromArtifactWithDiagnostics(
        string script,
        ICompiledQueryArtifact artifact,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompiledQueryArtifactLoadOptions? loadOptions,
        CompilationOptions? compilationOptions,
        CompiledQueryArtifactLoader? loader,
        CancellationToken cancellationToken)
    {
        return CreateExecutableFromArtifactWithDiagnosticsCore(
            script,
            artifact,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            loadOptions,
            loader == null ? null : loadedArtifact => loader(loadedArtifact),
            cancellationToken);
    }

    public static BuildResult CreateExecutableFromArtifactWithDiagnostics(
        string script,
        ICompiledQueryArtifact artifact,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompiledQueryArtifactLoadOptions? loadOptions,
        CompilationOptions? compilationOptions,
        CompiledQueryArtifactLoaderWithCancellation? loader,
        CancellationToken cancellationToken)
    {
        return CreateExecutableFromArtifactWithDiagnosticsCore(
            script,
            artifact,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            loadOptions,
            loader == null
                ? null
                : loadedArtifact => loader(loadedArtifact, cancellationToken),
            cancellationToken);
    }

    private static BuildResult CreateExecutableFromArtifactWithDiagnosticsCore(
        string script,
        ICompiledQueryArtifact artifact,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions,
        CompiledQueryArtifactLoadOptions? loadOptions,
        Func<ICompiledQueryArtifact, CompiledQueryArtifactLoadResult>? loader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(schemaProvider);
        ArgumentNullException.ThrowIfNull(loggerResolver);
        cancellationToken.ThrowIfCancellationRequested();

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
        cancellationToken.ThrowIfCancellationRequested();

        var effectiveLoadOptions = loadOptions ?? CompiledQueryArtifactLoadOptions.Default;
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryValidateLoadOptions(effectiveLoadOptions, artifactDiagnostics))
            return BuildResult.Failure(artifactDiagnostics, script);

        var effectiveCompilationOptions = compilationOptions ?? new CompilationOptions();
        cancellationToken.ThrowIfCancellationRequested();
        if (DiagnosticSqlCommandParser.TryParse(script, out var diagnosticCommand, out var parserDiagnostics))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (parserDiagnostics is { Count: > 0 })
                return BuildResult.Failure(parserDiagnostics.ToArray(), script);

            if (diagnosticCommand != null)
                return BuildResult.Failure(
                    [CreateArtifactDiagnostic("Diagnostic SQL commands cannot be loaded from reusable artifacts.")],
                    script);
        }
        cancellationToken.ThrowIfCancellationRequested();

        var diagnosticContext = new DiagnosticContext(new SourceText(script));
        var items = CreateBuildItems(script, assemblyName, schemaProvider, diagnosticContext, cancellationToken);
        items.CompilationPurpose = CompilationPurpose.ArtifactValidation;
        items.EmitPdb = false;
        items.CompilationOptions = effectiveCompilationOptions;
        ConfigureReusableArtifactRendering(items);
        items.StopAfterPlanning =
            effectiveLoadOptions.ValidationMode == CompiledQueryArtifactValidationMode.Fast;

        Exception? caughtException = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Build(items, CreateInspectionBuildChain(loggerResolver));
            RejectUnsupportedMultiStatementQuery(items.RawQueryTree);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (CompilationException ce)
        {
            cancellationToken.ThrowIfCancellationRequested();
            caughtException = ce;
            diagnosticContext.ReportException(ce);
        }
        catch (AstValidationException ave)
        {
            cancellationToken.ThrowIfCancellationRequested();
            caughtException = ave;
            diagnosticContext.ReportException(ave);
        }
        catch (MultiStatementQueryException mse)
        {
            cancellationToken.ThrowIfCancellationRequested();
            caughtException = mse;
            diagnosticContext.ReportException(mse);
        }
        catch (Exception ex) when (EvaluatorExceptionTaxonomy.IsExpectedQueryFailure(ex))
        {
            cancellationToken.ThrowIfCancellationRequested();
            caughtException = ex;
            diagnosticContext.ReportException(ex);
        }

        var diagnostics = diagnosticContext.Diagnostics.ToList();
        cancellationToken.ThrowIfCancellationRequested();
        if (diagnosticContext.HasErrors)
            return BuildResult.Failure(diagnostics, script, caughtException, items);

        CSharpClrCompiledArtifactLoader.ValidateArtifactCompatibility(
            script,
            artifact,
            assemblyName,
            effectiveCompilationOptions,
            effectiveLoadOptions.ValidationMode,
            items,
            diagnostics,
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (diagnostics.Any(static diagnostic => diagnostic.IsError))
            return BuildResult.Failure(diagnostics, script, buildItems: items);

        ClrLoadedExecutableArtifact? loadedExecutable = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            loadedExecutable = loader != null
                ? CSharpClrCompiledArtifactLoader.CreateLoadedExecutableArtifact(loader(artifact), cancellationToken)
                : CSharpClrCompiledArtifactLoader.LoadExecutableArtifactFromArtifactBytes(artifact, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            CSharpClrCompiledArtifactLoader.DisposeArtifactLifetime(loadedExecutable?.LifetimeOwner);
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            CSharpClrCompiledArtifactLoader.DisposeArtifactLifetime(loadedExecutable?.LifetimeOwner);
            cancellationToken.ThrowIfCancellationRequested();
            diagnostics.Add(CreateArtifactDiagnostic($"Compiled artifact type loading failed: {ex.Message}"));
            return BuildResult.Failure(diagnostics, script, ex, items);
        }

        try
        {
            CSharpClrCompiledArtifactLoader.ValidateLoadedRunnableType(
                artifact,
                loadedExecutable.RunnableType,
                diagnostics,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            CSharpClrCompiledArtifactLoader.DisposeArtifactLifetime(loadedExecutable.LifetimeOwner);
            throw;
        }
        catch
        {
            CSharpClrCompiledArtifactLoader.DisposeArtifactLifetime(loadedExecutable.LifetimeOwner);
            throw;
        }
        if (diagnostics.Any(static diagnostic => diagnostic.IsError))
        {
            CSharpClrCompiledArtifactLoader.DisposeArtifactLifetime(loadedExecutable.LifetimeOwner);
            return BuildResult.Failure(diagnostics, script, buildItems: items);
        }

        ITableRunnable? runnable = null;
        CompiledQuery? compiledQuery = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var activator = ExecutionTargetCatalog.ResolveActivator(loadedExecutable.TargetId);
            runnable = activator.ActivateTable(
                loadedExecutable,
                    new QueryRuntimeBinding(
                        items.SchemaProvider,
                        items.SourceRuntimeSettingsBySourceContextId,
                        items.SourceRuntimeSettingDescriptionsBySourceContextId,
                        CreateSourceExecutionPlans(items)));
            cancellationToken.ThrowIfCancellationRequested();
            runnable.Logger = loggerResolver.ResolveLogger();
            cancellationToken.ThrowIfCancellationRequested();
            compiledQuery = new CompiledQuery(runnable, loadedExecutable.LifetimeOwner);
            runnable = null;
            cancellationToken.ThrowIfCancellationRequested();
            return BuildResult.Success(compiledQuery, diagnostics, script, items);
        }
        catch (OperationCanceledException)
        {
            compiledQuery?.Dispose();
            (runnable as IDisposable)?.Dispose();
            if (compiledQuery == null)
                CSharpClrCompiledArtifactLoader.DisposeArtifactLifetime(loadedExecutable.LifetimeOwner);
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            compiledQuery?.Dispose();
            (runnable as IDisposable)?.Dispose();
            if (compiledQuery == null)
                CSharpClrCompiledArtifactLoader.DisposeArtifactLifetime(loadedExecutable.LifetimeOwner);
            cancellationToken.ThrowIfCancellationRequested();
            diagnostics.Add(CreateArtifactDiagnostic($"Compiled artifact runnable creation failed: {ex.Message}"));
            return BuildResult.Failure(diagnostics, script, ex, items);
        }
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
        CancellationToken cancellationToken,
        out Exception? caughtException)
    {
        caughtException = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ExecutionTargetCatalog.TryInspectArtifact(renderedArtifact, cancellationToken, out var inspection)
                ? inspection
                : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
