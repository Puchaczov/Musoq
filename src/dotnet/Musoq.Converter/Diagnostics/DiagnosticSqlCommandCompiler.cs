using System;
using System.Linq;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.IR.Execution;
using Musoq.Schema;

namespace Musoq.Converter.Diagnostics;

internal static class DiagnosticSqlCommandCompiler
{
    public static bool TryCompile(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions compilationOptions,
        out BuildResult? result)
    {
        result = null;

        if (!DiagnosticSqlCommandParser.TryParse(script, out var command, out var parserDiagnostics))
            return false;

        if (parserDiagnostics is { Count: > 0 })
        {
            result = BuildResult.Failure(parserDiagnostics.ToList(), script);
            return true;
        }

        if (command == null)
            return false;

        var innerOptions = CreateDiagnosticCompilationOptions(compilationOptions, command.Kind);
        var innerBuild = InstanceCreator.CompileWithDiagnostics(
            command.InnerScript,
            assemblyName,
            schemaProvider,
            loggerResolver,
            innerOptions,
            requireExecutionPlan: command.Kind == DiagnosticSqlCommandKind.ExplainAnalyze);

        if (!innerBuild.Succeeded)
        {
            result = innerBuild;
            return true;
        }

        var operatorCatalog = command.Kind == DiagnosticSqlCommandKind.ExplainAnalyze
            ? CreateOperatorCatalog(innerBuild.BuildItems)
            : null;

        var runnable = new DiagnosticSqlCommandRunnable(command.Kind, innerBuild.CompiledQuery, operatorCatalog);
        result = BuildResult.Success(
            new CompiledQuery(runnable),
            innerBuild.Diagnostics.ToArray(),
            script);
        return true;
    }

    private static CompilationOptions CreateDiagnosticCompilationOptions(
        CompilationOptions options,
        DiagnosticSqlCommandKind kind)
    {
        return kind == DiagnosticSqlCommandKind.ExplainAnalyze
            ? options.WithInstrumentationMode(QueryInstrumentationMode.Full)
            : options.InstrumentationMode == QueryInstrumentationMode.Disabled
                ? options.WithInstrumentationMode(QueryInstrumentationMode.SourceBoundaries)
                : options;
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
