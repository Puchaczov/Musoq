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
        if (DiagnosticSqlCommandCompiler.TryCompile(
                script,
                assemblyName,
                schemaProvider,
                loggerResolver,
                effectiveCompilationOptions,
                out var diagnosticCommandResult))
        {
            return diagnosticCommandResult!;
        }

        var cacheKey = !requireExecutionPlan &&
                       effectiveCompilationOptions.UsesDefaultSourceRuntimeSettingsResolver &&
                       CanUseExecutionCompilationCache(schemaProvider)
            ? CreateExecutionCompilationCacheKey(
                script,
                schemaProvider,
                effectiveCompilationOptions,
                ExecutionTargetIds.CSharpClr)
            : (ExecutionCompilationCacheKey?)null;

        if (cacheKey.HasValue &&
            TryCreateCachedExecutionBuildResult(
                script,
                assemblyName,
                schemaProvider,
                loggerResolver,
                effectiveCompilationOptions,
                cacheKey.Value) is { } cachedResult)
            return cachedResult;

        var diagnosticContext = new DiagnosticContext(new SourceText(script));

        var items = CreateBuildItems(script, assemblyName, schemaProvider, diagnosticContext);
        items.EmitPdb = Debugger.IsAttached;
        items.EmitExecutionPlanText = requireExecutionPlan;
        items.CompilationOptions = effectiveCompilationOptions;

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
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            caughtException = ex;
            diagnosticContext.ReportException(ex);
        }

        var diagnostics = diagnosticContext.Diagnostics.ToList();

        if (diagnosticContext.HasErrors)
            return BuildResult.Failure(diagnostics, script, caughtException, items);

        var runnableType = LoadRunnableType(items);
        var runnable = CreateRunnable(
            runnableType,
            new QueryRuntimeBinding(
                items.SchemaProvider,
                items.SourceRuntimeSettingsBySourceContextId,
                items.SourceRuntimeSettingDescriptionsBySourceContextId,
                CreateSourceExecutionPlans(items)));
        runnable.Logger = loggerResolver.ResolveLogger();

        if (cacheKey.HasValue && CanUseExecutionCompilationCache(items))
        {
            StoreExecutionCompilation(
                cacheKey.Value,
                CreateCachedExecutableArtifact(cacheKey.Value.ExecutionTarget, runnableType));
        }

        return BuildResult.Success(new CompiledQuery(runnable), diagnostics, script, items);
    }
}
