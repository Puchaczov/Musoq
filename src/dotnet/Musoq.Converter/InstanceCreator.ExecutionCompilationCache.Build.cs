using System.Diagnostics;
using System.Linq;
using System.Threading;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    private static BuildResult? TryCreateCachedExecutionBuildResult(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions compilationOptions,
        ExecutionCompilationCacheKey cacheKey,
        EvaluatorPerformanceTelemetry.CompilationScope telemetry,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var lookupStarted = Stopwatch.GetTimestamp();
        using var cacheReader = TryAcquireExecutionCompilationReader(cacheKey, out var cachedCompilation);
        if (cacheReader is null || cachedCompilation is null)
        {
            telemetry.AddPhase("cache-lookup", lookupStarted);
            return null;
        }

        cachedCompilation.Touch();
        telemetry.AddPhase("cache-lookup", lookupStarted);

        var diagnosticContext = new DiagnosticContext(new SourceText(script));
        var items = CreateBuildItems(script, assemblyName, schemaProvider, diagnosticContext, cancellationToken);
        items.EmitPdb = Debugger.IsAttached;
        items.CompilationOptions = compilationOptions;
        items.EnableContextualExecution = true;
        items.StopAfterPlanning = true;

        Exception? caughtException = null;
        var cachedBuildStarted = Stopwatch.GetTimestamp();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Build(items, CreateExecutableBuildChain(loggerResolver));
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
        finally
        {
            telemetry.AddPhase("cache-hit-planning", cachedBuildStarted);
        }

        var diagnostics = diagnosticContext.Diagnostics.ToList();
        cancellationToken.ThrowIfCancellationRequested();
        if (diagnosticContext.HasErrors)
            return BuildResult.Failure(diagnostics, script, caughtException, items);

        var semanticContractFingerprint = CreateSemanticExecutionContractFingerprint(
            items,
            schemaProvider,
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        telemetry.SetSemanticContractFingerprint(semanticContractFingerprint);
        if (!string.Equals(
                cachedCompilation.SemanticContractFingerprint,
                semanticContractFingerprint,
                StringComparison.Ordinal))
            return null;

        if (!CanUseExecutionCompilationCache(items))
        {
            return null;
        }

        cachedCompilation.Touch();

        ITableRunnable? runnable = null;
        CompiledQuery? compiledQuery = null;
        try
        {
            var runnableStarted = Stopwatch.GetTimestamp();
            cancellationToken.ThrowIfCancellationRequested();
            runnable = CreateRunnable(cachedCompilation, items);
            runnable.Logger = loggerResolver.ResolveLogger();
            cancellationToken.ThrowIfCancellationRequested();
            telemetry.AddPhase("cache-hit-runnable", runnableStarted);
            telemetry.SetArtifactIdentity(
                cachedCompilation.Template.RunnableTypeName,
                emitted: false,
                loaded: false);
            telemetry.SetBindingIdentity($"{schemaProvider.GetType().AssemblyQualifiedName}|{items.QueryResultMode}");
            compiledQuery = new CompiledQuery(runnable);
            runnable = null;
            cancellationToken.ThrowIfCancellationRequested();

            return BuildResult.Success(compiledQuery, diagnostics, script, items);
        }
        catch (OperationCanceledException)
        {
            compiledQuery?.Dispose();
            (runnable as IDisposable)?.Dispose();
            throw;
        }
        catch
        {
            compiledQuery?.Dispose();
            (runnable as IDisposable)?.Dispose();
            throw;
        }
    }
}
