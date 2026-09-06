using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        bool requireExecutionPlan,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schemaProvider);
        ArgumentNullException.ThrowIfNull(loggerResolver);
        cancellationToken.ThrowIfCancellationRequested();
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
            telemetry.SetProviderSignature(CreateProviderSignature(schemaProvider, cancellationToken));

        var diagnosticCompilationStarted = Stopwatch.GetTimestamp();
        if (DiagnosticSqlCommandCompiler.TryCompile(
                script,
                assemblyName,
                schemaProvider,
                loggerResolver,
                effectiveCompilationOptions,
                cancellationToken,
                out var diagnosticCommandResult))
        {
            cancellationToken.ThrowIfCancellationRequested();
            telemetry.AddPhase("diagnostic-command", diagnosticCompilationStarted);
            telemetry.SetCacheOutcome("diagnostic-command");
            return diagnosticCommandResult!;
        }
        telemetry.AddPhase("diagnostic-command", diagnosticCompilationStarted);
        cancellationToken.ThrowIfCancellationRequested();

        var cacheKeyStarted = Stopwatch.GetTimestamp();
        var cacheKey = !requireExecutionPlan &&
                       effectiveCompilationOptions.UsesDefaultSourceRuntimeSettingsResolver &&
                       CanUseExecutionCompilationCache(schemaProvider)
            ? CreateExecutionCompilationCacheKey(
                script,
                schemaProvider,
                effectiveCompilationOptions,
                ExecutionTargetIds.CSharpClr,
                cancellationToken: cancellationToken)
            : (ExecutionCompilationCacheKey?)null;
        telemetry.CacheEligible = cacheKey.HasValue;
        if (cacheKey.HasValue)
            telemetry.SetProviderContractBucket(cacheKey.Value.ProviderContractBucket);
        telemetry.AddPhase("cache-key", cacheKeyStarted);

        using var executionCompilationFlight = cacheKey.HasValue
            ? AcquireExecutionCompilationFlight(cacheKey.Value, cancellationToken)
            : null;

        if (cacheKey.HasValue &&
            TryCreateCachedExecutionBuildResult(
                script,
                assemblyName,
                schemaProvider,
                loggerResolver,
                effectiveCompilationOptions,
                cacheKey.Value,
                telemetry,
                cancellationToken) is { } cachedResult)
        {
            telemetry.SetCacheOutcome("hit");
            return cachedResult;
        }

        if (cacheKey.HasValue)
            telemetry.SetCacheOutcome("miss");

        var diagnosticContext = new DiagnosticContext(new SourceText(script));

        var items = CreateBuildItems(script, assemblyName, schemaProvider, diagnosticContext, cancellationToken);
        items.EmitPdb = Debugger.IsAttached;
        items.EmitExecutionPlanText = requireExecutionPlan;
        items.CompilationPurpose = CompilationPurpose.Execution;
        items.CompilationOptions = effectiveCompilationOptions;
        items.EnableContextualExecution = true;
        items.RetainSemanticCacheState = true;
        ExecutionCompilationCachePublication? cachePublication = null;

        try
        {
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
        catch (Exception ex)
        {
            cancellationToken.ThrowIfCancellationRequested();
            caughtException = ex;
            if (!diagnosticContext.HasErrors)
                diagnosticContext.ReportException(InternalDiagnosticException.ForCompiler(ex));
        }

        var diagnostics = diagnosticContext.Diagnostics.ToList();
        cancellationToken.ThrowIfCancellationRequested();

        if (diagnosticContext.HasErrors)
            return BuildResult.Failure(diagnostics, script, caughtException, items);

        cancellationToken.ThrowIfCancellationRequested();
        var semanticContractFingerprint = cacheKey.HasValue && CanUseExecutionCompilationCache(items)
            ? CreateSemanticExecutionContractFingerprint(items, schemaProvider, cancellationToken)
            : null;
        cancellationToken.ThrowIfCancellationRequested();
        if (semanticContractFingerprint is not null)
            telemetry.SetSemanticContractFingerprint(semanticContractFingerprint);

        CanonicalExecutionArtifactContract? canonicalContract;
        using (EvaluatorPerformanceTelemetry.BeginPhase("canonical-identity"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            canonicalContract = semanticContractFingerprint is not null &&
                                CanUseCanonicalExecutionCompilationCache(items)
                ? CreateCanonicalExecutionArtifactContract(
                    items,
                    schemaProvider,
                    effectiveCompilationOptions,
                    cancellationToken)
                : null;
            cancellationToken.ThrowIfCancellationRequested();
        }

        using var canonicalCompilationFlight = canonicalContract is not null
            ? AcquireCanonicalExecutionCompilationFlight(canonicalContract, cancellationToken)
            : null;
        CachedExecutionCompilation? canonicalCompilation = null;
        IDisposable? canonicalCompilationReader = null;
        if (canonicalContract is not null)
            canonicalCompilationReader = TryAcquireCanonicalExecutionCompilationReader(
                canonicalContract,
                out canonicalCompilation);
        using var canonicalReader = canonicalCompilationReader;

        if (canonicalCompilation is not null)
        {
            items.ExecutableArtifact = canonicalCompilation.Template.ExecutableArtifact;

            ExecutionCompilationCachePublication? canonicalAliasPublication = null;
            if (cacheKey.HasValue)
            {
                canonicalAliasPublication = PrepareCanonicalExecutionAlias(
                    cacheKey.Value,
                    canonicalCompilation,
                    canonicalContract!,
                    cancellationToken);
            }

            ITableRunnable? canonicalRunnable = null;
            CompiledQuery? canonicalQuery = null;
            try
            {
                var canonicalRunnableStarted = Stopwatch.GetTimestamp();
                canonicalRunnable = CreateRunnable(canonicalCompilation, items);
                cancellationToken.ThrowIfCancellationRequested();
                canonicalRunnable.Logger = loggerResolver.ResolveLogger();
                cancellationToken.ThrowIfCancellationRequested();
                telemetry.AddPhase("canonical-cache-hit-runnable", canonicalRunnableStarted);
                telemetry.SetArtifactIdentity(
                    canonicalCompilation.Template.RunnableTypeName,
                    emitted: false,
                    loaded: false);
                telemetry.SetBindingIdentity($"{schemaProvider.GetType().AssemblyQualifiedName}|{items.QueryResultMode}");
                telemetry.SetCacheOutcome("canonical-hit");
                canonicalQuery = new CompiledQuery(canonicalRunnable);
                canonicalRunnable = null;
                if (canonicalAliasPublication is not null)
                    InvokeExecutionCompilationCommitTestHook();
                cancellationToken.ThrowIfCancellationRequested();
                if (canonicalAliasPublication is not null)
                    CommitCanonicalExecutionAliasAfterCancellationCheck(canonicalAliasPublication);
                CompleteSemanticCache(items, commit: true, cancellationToken: CancellationToken.None);
                return BuildResult.Success(
                    canonicalQuery,
                    diagnosticContext.Diagnostics.ToList(),
                    script,
                    items);
            }
            catch (OperationCanceledException)
            {
                // A canonical cache hit may have created a runnable before the
                // caller cancelled. Do not leave that instance alive.
                canonicalQuery?.Dispose();
                (canonicalRunnable as IDisposable)?.Dispose();
                throw;
            }
            catch (Exception ex)
            {
                canonicalQuery?.Dispose();
                (canonicalRunnable as IDisposable)?.Dispose();
                cancellationToken.ThrowIfCancellationRequested();
                caughtException = ex;
                diagnosticContext.ReportException(InternalDiagnosticException.ForCompiler(ex));
                return BuildResult.Failure(
                    diagnosticContext.Diagnostics.ToList(),
                    script,
                    caughtException,
                    items);
            }
            finally
            {
                canonicalAliasPublication?.Dispose();
            }
        }

        if (canonicalContract is not null)
            telemetry.SetCacheOutcome("canonical-miss");

        try
        {
            using (EvaluatorPerformanceTelemetry.BeginPhase("emission"))
                FinalizeExecutionArtifacts(items);
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (CompilationException ce)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
            cancellationToken.ThrowIfCancellationRequested();
            caughtException = ex;
            diagnosticContext.ReportException(InternalDiagnosticException.ForCompiler(ex));
            return BuildResult.Failure(diagnosticContext.Diagnostics.ToList(), script, caughtException, items);
        }

        ITableRunnable? runnable = null;
        CompiledQuery? compiledQuery = null;
        try
        {
            var runnableStarted = Stopwatch.GetTimestamp();
            Type? runnableType = null;
            string runnableTypeName;
            if (ContainsQueryScopedRowTransfer(items))
            {
                using (EvaluatorPerformanceTelemetry.BeginPhase("activate-query-row-runnable"))
                    runnable = CreateRunnable(items);
                runnableTypeName = items.AccessToClassPath;
                cancellationToken.ThrowIfCancellationRequested();
            }
            else
            {
                using (EvaluatorPerformanceTelemetry.BeginPhase("load-runnable-type"))
                    runnableType = LoadRunnableType(items);
                cancellationToken.ThrowIfCancellationRequested();
                runnable = CreateRunnable(
                    runnableType,
                    CreateRuntimeBinding(items));
                runnableTypeName = runnableType.FullName ?? runnableType.Name;
            }

            cancellationToken.ThrowIfCancellationRequested();
            runnable.Logger = loggerResolver.ResolveLogger();
            cancellationToken.ThrowIfCancellationRequested();
            telemetry.AddPhase("create-runnable", runnableStarted);
            telemetry.SetArtifactIdentity(runnableTypeName, emitted: true, loaded: true);
            telemetry.SetBindingIdentity($"{items.SchemaProvider.GetType().AssemblyQualifiedName}|{items.QueryResultMode}");

            if (cacheKey.HasValue && CanUseExecutionCompilationCache(items))
            {
                var cacheStoreStarted = Stopwatch.GetTimestamp();
                var cacheArtifact = CreateCachedExecutableArtifact(
                    items,
                    cacheKey.Value.ExecutionTarget,
                    runnableType,
                    cancellationToken);
                try
                {
                    cachePublication = PrepareExecutionCompilation(
                        cacheKey.Value,
                        cacheArtifact,
                        semanticContractFingerprint!,
                        runnableTypeName,
                        canonicalContract,
                        cancellationToken);
                    cacheArtifact = null!;
                }
                finally
                {
                    (cacheArtifact as IDisposable)?.Dispose();
                }

                telemetry.AddPhase("cache-store", cacheStoreStarted);
            }

            cancellationToken.ThrowIfCancellationRequested();
            compiledQuery = new CompiledQuery(runnable);
            runnable = null;
            if (cachePublication is not null)
                InvokeExecutionCompilationCommitTestHook();
            cancellationToken.ThrowIfCancellationRequested();
            if (cachePublication is not null)
                CommitExecutionCompilationAfterCancellationCheck(cachePublication);
            CompleteSemanticCache(items, commit: true, cancellationToken: CancellationToken.None);
            return BuildResult.Success(compiledQuery, diagnosticContext.Diagnostics.ToList(), script, items);
        }
        catch (OperationCanceledException)
        {
            compiledQuery?.Dispose();
            (runnable as IDisposable)?.Dispose();
            throw;
        }
        catch (Exception ex)
        {
            compiledQuery?.Dispose();
            (runnable as IDisposable)?.Dispose();
            cancellationToken.ThrowIfCancellationRequested();
            caughtException = ex;
            diagnosticContext.ReportException(InternalDiagnosticException.ForCompiler(ex));
            return BuildResult.Failure(
                diagnosticContext.Diagnostics.ToList(),
                script,
                caughtException,
                items);
        }
        }
        finally
        {
            cachePublication?.Dispose();
            CompleteSemanticCache(items, commit: false);
        }
    }
}
