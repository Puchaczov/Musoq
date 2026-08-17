using System.Diagnostics;
using System.Linq;
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
        EvaluatorPerformanceTelemetry.CompilationScope telemetry)
    {
        var lookupStarted = Stopwatch.GetTimestamp();
        if (!ExecutionCompilationCache.TryGetValue(cacheKey, out var cachedCompilation))
        {
            telemetry.AddPhase("cache-lookup", lookupStarted);
            return null;
        }

        cachedCompilation.Touch();
        telemetry.AddPhase("cache-lookup", lookupStarted);

        var diagnosticContext = new DiagnosticContext(new SourceText(script));
        var items = CreateBuildItems(script, assemblyName, schemaProvider, diagnosticContext);
        items.EmitPdb = Debugger.IsAttached;
        items.CompilationOptions = compilationOptions;
        items.EnableContextualExecution = true;
        items.StopAfterPlanning = true;

        Exception? caughtException = null;
        var cachedBuildStarted = Stopwatch.GetTimestamp();
        try
        {
            Build(items, CreateExecutableBuildChain(loggerResolver));
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
        finally
        {
            telemetry.AddPhase("cache-hit-planning", cachedBuildStarted);
        }

        var diagnostics = diagnosticContext.Diagnostics.ToList();
        if (diagnosticContext.HasErrors)
            return BuildResult.Failure(diagnostics, script, caughtException, items);

        var semanticContractFingerprint = CreateSemanticExecutionContractFingerprint(items, schemaProvider);
        telemetry.SetSemanticContractFingerprint(semanticContractFingerprint);
        if (!string.Equals(
                cachedCompilation.SemanticContractFingerprint,
                semanticContractFingerprint,
                StringComparison.Ordinal))
            return null;

        if (!CanUseExecutionCompilationCache(items) ||
            !ExecutionCompilationCache.TryGetValue(cacheKey, out cachedCompilation))
        {
            return null;
        }

        cachedCompilation.Touch();

        var runnableStarted = Stopwatch.GetTimestamp();
        var runnable = CreateRunnable(cachedCompilation, items);
        runnable.Logger = loggerResolver.ResolveLogger();
        telemetry.AddPhase("cache-hit-runnable", runnableStarted);
        telemetry.SetArtifactIdentity(
            cachedCompilation.Template.RunnableTypeName,
            emitted: false,
            loaded: false);
        telemetry.SetBindingIdentity($"{schemaProvider.GetType().AssemblyQualifiedName}|{items.QueryResultMode}");

        return BuildResult.Success(new CompiledQuery(runnable), diagnostics, script, items);
    }
}
