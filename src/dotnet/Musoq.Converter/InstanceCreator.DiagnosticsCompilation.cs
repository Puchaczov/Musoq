using System.Diagnostics;
using System.Linq;
using Musoq.Converter.Build;
using Musoq.Converter.Diagnostics;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    private static BuildResult CompileWithDiagnosticsCore(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions,
        bool requireExecutionPlan)
    {
        ArgumentNullException.ThrowIfNull(schemaProvider);
        ArgumentNullException.ThrowIfNull(loggerResolver);
        if (string.IsNullOrWhiteSpace(script))
        {
            var emptySourceText = new SourceText(script);
            var emptyContext = new DiagnosticContext(emptySourceText);
            emptyContext.ReportError(
                DiagnosticCode.MQ2016_IncompleteStatement,
                "The query is empty. Provide a valid SQL query starting with SELECT, WITH, DESC, TABLE, COUPLE, or an optional param(...) block.",
                TextSpan.Empty);
            return BuildResult.Failure(emptyContext.Diagnostics.ToList(), script ?? string.Empty);
        }

        var effectiveCompilationOptions = compilationOptions ?? new CompilationOptions();
        using var telemetry = EvaluatorPerformanceTelemetry.BeginCompilation(
            script,
            assemblyName,
            schemaProvider,
            effectiveCompilationOptions);
        if (EvaluatorPerformanceTelemetry.IsEnabled)
            telemetry.SetProviderSignature(CreateProviderSignature(schemaProvider));

        var diagnosticCompilationStarted = Stopwatch.GetTimestamp();
        if (DiagnosticSqlCommandCompiler.TryCompile(
                script,
                assemblyName,
                schemaProvider,
                loggerResolver,
                effectiveCompilationOptions,
                out var diagnosticCommandResult))
        {
            telemetry.AddPhase("diagnostic-command", diagnosticCompilationStarted);
            telemetry.SetCacheOutcome("diagnostic-command");
            return diagnosticCommandResult!;
        }
        telemetry.AddPhase("diagnostic-command", diagnosticCompilationStarted);

        var cacheKeyStarted = Stopwatch.GetTimestamp();
        var cacheKey = !requireExecutionPlan &&
                       effectiveCompilationOptions.UsesDefaultSourceRuntimeSettingsResolver &&
                       CanUseExecutionCompilationCache(schemaProvider)
            ? CreateExecutionCompilationCacheKey(
                script,
                schemaProvider,
                effectiveCompilationOptions,
                ExecutionTargetIds.CSharpClr)
            : (ExecutionCompilationCacheKey?)null;
        telemetry.CacheEligible = cacheKey.HasValue;
        if (cacheKey.HasValue)
            telemetry.SetProviderContractBucket(cacheKey.Value.ProviderContractBucket);
        telemetry.AddPhase("cache-key", cacheKeyStarted);

        using var executionCompilationFlight = cacheKey.HasValue
            ? AcquireExecutionCompilationFlight(cacheKey.Value)
            : null;

        if (cacheKey.HasValue &&
            TryCreateCachedExecutionBuildResult(
                script,
                assemblyName,
                schemaProvider,
                loggerResolver,
                effectiveCompilationOptions,
                cacheKey.Value,
                telemetry) is { } cachedResult)
        {
            telemetry.SetCacheOutcome("hit");
            return cachedResult;
        }

        if (cacheKey.HasValue)
            telemetry.SetCacheOutcome("miss");

        var diagnosticContext = new DiagnosticContext(new SourceText(script));

        var items = CreateBuildItems(script, assemblyName, schemaProvider, diagnosticContext);
        items.EmitPdb = Debugger.IsAttached;
        items.EmitExecutionPlanText = requireExecutionPlan;
        items.CompilationPurpose = CompilationPurpose.Execution;
        items.CompilationOptions = effectiveCompilationOptions;
        items.EnableContextualExecution = true;

        Exception? caughtException = null;
        try
        {
            using var buildPhase = EvaluatorPerformanceTelemetry.BeginPhase("build");
            // Render first. Finalization is deliberately kept outside the
            // semantic/rendering pipeline so a canonical artifact can be
            // reused without another Roslyn emission.
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
        catch (Exception ex)
        {
            caughtException = ex;
            if (!diagnosticContext.HasErrors)
                diagnosticContext.ReportException(InternalDiagnosticException.ForCompiler(ex));
        }

        var diagnostics = diagnosticContext.Diagnostics.ToList();

        if (diagnosticContext.HasErrors)
            return BuildResult.Failure(diagnostics, script, caughtException, items);

        var semanticContractFingerprint = cacheKey.HasValue && CanUseExecutionCompilationCache(items)
            ? CreateSemanticExecutionContractFingerprint(items, schemaProvider)
            : null;
        if (semanticContractFingerprint is not null)
            telemetry.SetSemanticContractFingerprint(semanticContractFingerprint);

        CanonicalExecutionArtifactContract? canonicalContract;
        using (EvaluatorPerformanceTelemetry.BeginPhase("canonical-identity"))
        {
            canonicalContract = semanticContractFingerprint is not null &&
                                CanUseCanonicalExecutionCompilationCache(items)
                ? CreateCanonicalExecutionArtifactContract(
                    items,
                    schemaProvider,
                    effectiveCompilationOptions)
                : null;
        }

        using var canonicalCompilationFlight = canonicalContract is not null
            ? AcquireCanonicalExecutionCompilationFlight(canonicalContract)
            : null;

        if (canonicalContract is not null &&
            TryGetCanonicalExecutionCompilation(canonicalContract) is { } canonicalCompilation)
        {
            canonicalCompilation.Touch();
            items.ExecutableArtifact = canonicalCompilation.Template.ExecutableArtifact;

            if (cacheKey.HasValue)
            {
                StoreCanonicalExecutionAlias(
                    cacheKey.Value,
                    canonicalCompilation,
                    canonicalContract);
            }

            try
            {
                var canonicalRunnableStarted = Stopwatch.GetTimestamp();
                var canonicalRunnable = CreateRunnable(canonicalCompilation, items);
                canonicalRunnable.Logger = loggerResolver.ResolveLogger();
                telemetry.AddPhase("canonical-cache-hit-runnable", canonicalRunnableStarted);
                telemetry.SetArtifactIdentity(
                    canonicalCompilation.Template.RunnableTypeName,
                    emitted: false,
                    loaded: false);
                telemetry.SetBindingIdentity($"{schemaProvider.GetType().AssemblyQualifiedName}|{items.QueryResultMode}");
                telemetry.SetCacheOutcome("canonical-hit");
                return BuildResult.Success(
                    new CompiledQuery(canonicalRunnable),
                    diagnosticContext.Diagnostics.ToList(),
                    script,
                    items);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                caughtException = ex;
                diagnosticContext.ReportException(InternalDiagnosticException.ForCompiler(ex));
                return BuildResult.Failure(
                    diagnosticContext.Diagnostics.ToList(),
                    script,
                    caughtException,
                    items);
            }
        }

        if (canonicalContract is not null)
            telemetry.SetCacheOutcome("canonical-miss");

        try
        {
            using (EvaluatorPerformanceTelemetry.BeginPhase("emission"))
                FinalizeExecutionArtifacts(items);
        }
        catch (CompilationException ce)
        {
            caughtException = ce;
            diagnosticContext.ReportException(ce);
            return BuildResult.Failure(diagnosticContext.Diagnostics.ToList(), script, caughtException, items);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            caughtException = ex;
            diagnosticContext.ReportException(InternalDiagnosticException.ForCompiler(ex));
            return BuildResult.Failure(diagnosticContext.Diagnostics.ToList(), script, caughtException, items);
        }

        try
        {
            Type runnableType;
            using (EvaluatorPerformanceTelemetry.BeginPhase("load-runnable-type"))
                runnableType = LoadRunnableType(items);

            var runnableStarted = Stopwatch.GetTimestamp();
            var runnable = CreateRunnable(
                runnableType,
                CreateRuntimeBinding(items));
            runnable.Logger = loggerResolver.ResolveLogger();
            telemetry.AddPhase("create-runnable", runnableStarted);
            telemetry.SetArtifactIdentity(runnableType.FullName ?? runnableType.Name, emitted: true, loaded: true);
            telemetry.SetBindingIdentity($"{items.SchemaProvider.GetType().AssemblyQualifiedName}|{items.QueryResultMode}");

            if (cacheKey.HasValue && CanUseExecutionCompilationCache(items))
            {
                var cacheStoreStarted = Stopwatch.GetTimestamp();
                StoreExecutionCompilation(
                    cacheKey.Value,
                    CreateCachedExecutableArtifact(cacheKey.Value.ExecutionTarget, runnableType),
                    semanticContractFingerprint!,
                    runnableType.FullName ?? runnableType.Name,
                    canonicalContract);
                telemetry.AddPhase("cache-store", cacheStoreStarted);
            }

            return BuildResult.Success(new CompiledQuery(runnable), diagnosticContext.Diagnostics.ToList(), script, items);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            caughtException = ex;
            diagnosticContext.ReportException(InternalDiagnosticException.ForCompiler(ex));
            return BuildResult.Failure(
                diagnosticContext.Diagnostics.ToList(),
                script,
                caughtException,
                items);
        }
    }
}
