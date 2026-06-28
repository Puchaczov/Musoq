using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tables;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Converter.Diagnostics;

internal sealed class DiagnosticSqlCommandRunnable(
    DiagnosticSqlCommandKind kind,
    CompiledQuery innerQuery,
    ExecutionPlanOperatorCatalog? operatorCatalog = null) : ITableRunnable, IParameterizedRunnable
{
    public ISchemaProvider Provider { get; set; } = null!;

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; } =
        new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; } =
        new Dictionary<string, SourceExecutionPlan>(StringComparer.Ordinal);

    public ILogger Logger { get; set; } = null!;

    public IDictionary<string, object?> Parameters => innerQuery.Parameters;

    public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions => innerQuery.ParameterDefinitions;

    public IReadOnlyList<ScriptParameterContract> ParameterContracts => innerQuery.ParameterContracts;

    public event QueryPhaseEventHandler PhaseChanged
    {
        add => innerQuery.PhaseChanged += value;
        remove => innerQuery.PhaseChanged -= value;
    }

    public event DataSourceEventHandler DataSourceProgress
    {
        add => innerQuery.DataSourceProgress += value;
        remove => innerQuery.DataSourceProgress -= value;
    }

    public Table Run(CancellationToken token)
    {
        var text = kind switch
        {
            DiagnosticSqlCommandKind.Profile => innerQuery.RunWithProfile(token).ProfileText,
            DiagnosticSqlCommandKind.ExplainAnalyze => CreateExplainAnalyzeText(token),
            _ => throw new NotSupportedException($"Diagnostic command kind {kind} is not supported.")
        };

        return CreateTextTable(text);
    }

    private string CreateExplainAnalyzeText(CancellationToken token)
    {
        if (operatorCatalog == null)
            throw new InvalidOperationException("EXPLAIN ANALYZE requires an execution plan.");

        var profileResult = innerQuery.RunWithProfile(token, emitTelemetry: false);
        var operators = ExecutionPlanOperatorIdAnnotator.CreateOperatorSnapshots(operatorCatalog, profileResult.Profile);
        var profile = profileResult.Profile with { Operators = operators };
        QueryProfileTelemetry.Emit(profile);

        return ExplainAnalyzeTextPrinter.Print(operatorCatalog.AnnotatedExecutionPlanText, profile);
    }

    private static Table CreateTextTable(string text)
    {
        var table = new Table(
            "diagnostics",
            [
                new Column("LineNumber", typeof(int), 0),
                new Column("Text", typeof(string), 1)
            ]);
        var lines = SplitLines(text).ToArray();

        for (var index = 0; index < lines.Length; index++)
            table.AddDirect(new DiagnosticTextRow(index + 1, lines[index]));

        return table;
    }

    private static IEnumerable<string> SplitLines(string text)
    {
        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .TrimEnd('\n')
            .Split('\n');
    }

    private sealed class DiagnosticTextRow(int lineNumber, string text) : Row
    {
        public override int Count => 2;

        public override object this[int columnNumber] => columnNumber switch
        {
            0 => lineNumber,
            1 => text,
            _ => throw new IndexOutOfRangeException()
        };
    }
}
