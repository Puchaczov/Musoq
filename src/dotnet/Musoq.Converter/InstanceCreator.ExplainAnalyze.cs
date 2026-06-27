using System.Threading;
using Musoq.Converter.Build;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.IR.Execution;
using Musoq.Schema;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    public static ExplainAnalyzeResult ExplainAnalyze(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver)
    {
        return ExplainAnalyze(script, assemblyName, schemaProvider, loggerResolver, null, CancellationToken.None);
    }

    public static ExplainAnalyzeResult ExplainAnalyze(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CancellationToken token)
    {
        return ExplainAnalyze(script, assemblyName, schemaProvider, loggerResolver, null, token);
    }

    public static ExplainAnalyzeResult ExplainAnalyze(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions)
    {
        return ExplainAnalyze(script, assemblyName, schemaProvider, loggerResolver, compilationOptions, CancellationToken.None);
    }

    public static ExplainAnalyzeResult ExplainAnalyze(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions,
        CancellationToken token)
    {
        var options = CreateExplainAnalyzeCompilationOptions(compilationOptions);
        var build = CompileWithDiagnostics(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            options,
            requireExecutionPlan: true);

        if (!build.Succeeded)
        {
            throw build.CaughtException != null
                ? new MusoqQueryException(build.ToEnvelopes(), build.CaughtException)
                : new MusoqQueryException(build.ToEnvelopes());
        }

        var profileResult = build.CompiledQuery.RunWithProfile(token, emitTelemetry: false);
        var catalog = CreateOperatorCatalog(build.BuildItems);
        var executionPlanText = catalog.AnnotatedExecutionPlanText;
        var operators = ExecutionPlanOperatorIdAnnotator.CreateOperatorSnapshots(catalog, profileResult.Profile);
        var profile = profileResult.Profile with { Operators = operators };
        QueryProfileTelemetry.Emit(profile);

        return new ExplainAnalyzeResult(
            profileResult.Result,
            profile,
            executionPlanText,
            ExplainAnalyzeTextPrinter.Print(executionPlanText, profile));
    }

    private static CompilationOptions CreateExplainAnalyzeCompilationOptions(CompilationOptions? compilationOptions)
    {
        var options = compilationOptions ?? new CompilationOptions();
        return options.InstrumentationMode == QueryInstrumentationMode.Full
            ? options
            : options.WithInstrumentationMode(QueryInstrumentationMode.Full);
    }

    private static ExecutionPlanOperatorCatalog CreateOperatorCatalog(BuildItems? buildItems)
    {
        if (buildItems?.ExecutionPlan != null)
            return ExecutionPlanOperatorCatalog.Create(buildItems.ExecutionPlan);

        if (!string.IsNullOrWhiteSpace(buildItems?.ExecutionPlanText))
            return ExecutionPlanOperatorCatalog.Create(buildItems.ExecutionPlanText);

        throw new InvalidOperationException("EXPLAIN ANALYZE compilation did not produce an execution plan.");
    }
}
