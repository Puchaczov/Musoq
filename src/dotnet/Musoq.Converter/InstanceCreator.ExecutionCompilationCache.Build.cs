using System.Diagnostics;
using System.Linq;
using Musoq.Converter.Build;
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
        ExecutionCompilationCacheKey cacheKey)
    {
        if (!ExecutionCompilationCache.TryGetValue(cacheKey, out var cachedCompilation))
            return null;

        cachedCompilation.Touch();

        var diagnosticContext = new DiagnosticContext(new SourceText(script));
        var items = CreateBuildItems(script, assemblyName, schemaProvider, diagnosticContext);
        items.EmitPdb = Debugger.IsAttached;
        items.CompilationOptions = compilationOptions;
        items.EnableContextualExecution = true;
        items.StopAfterPlanning = true;

        Exception? caughtException = null;
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

        var diagnostics = diagnosticContext.Diagnostics.ToList();
        if (diagnosticContext.HasErrors)
            return BuildResult.Failure(diagnostics, script, caughtException, items);

        if (!CanUseExecutionCompilationCache(items) ||
            !ExecutionCompilationCache.TryGetValue(cacheKey, out cachedCompilation))
        {
            return null;
        }

        cachedCompilation.Touch();

        var runnable = CreateRunnable(cachedCompilation, items);
        runnable.Logger = loggerResolver.ResolveLogger();

        return BuildResult.Success(new CompiledQuery(runnable), diagnostics, script, items);
    }
}
