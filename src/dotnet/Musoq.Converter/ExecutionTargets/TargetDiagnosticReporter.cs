using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Build;

internal static class TargetDiagnosticReporter
{
    public static void Report(
        IEnumerable<TargetDiagnostic> targetDiagnostics,
        DiagnosticContext context)
    {
        ArgumentNullException.ThrowIfNull(targetDiagnostics);
        ArgumentNullException.ThrowIfNull(context);

        context.AddRange(targetDiagnostics.Select(Convert));
    }

    private static Diagnostic Convert(TargetDiagnostic diagnostic)
    {
        var (start, end) = ResolveLocations(diagnostic);
        return new Diagnostic(
            DiagnosticCode.MQ8001_CodeGenerationFailed,
            MapSeverity(diagnostic.Severity),
            $"[{diagnostic.Code}] {diagnostic.Message}",
            start,
            end,
            diagnostic.SourceSnippet,
            phase: DiagnosticPhase.CodeGeneration,
            sourceKind: DiagnosticSourceKind.GeneratedSource,
            arguments:
            [
                new KeyValuePair<string, string>("targetCode", diagnostic.Code)
            ]);
    }

    private static (SourceLocation Start, SourceLocation End) ResolveLocations(TargetDiagnostic diagnostic)
    {
        if (diagnostic.SourceRange is not { } range)
            return (SourceLocation.None, SourceLocation.None);

        var sourceName = diagnostic.SourceName ?? "<generated>";
        var start = CreateLocation(
            range.Start,
            range.StartLine,
            range.StartColumn,
            sourceName);
        var end = CreateLocation(
            range.End,
            range.EndLine,
            range.EndColumn,
            sourceName);
        return (start, end);
    }

    private static SourceLocation CreateLocation(
        int offset,
        int? line,
        int? column,
        string sourceName)
    {
        if (line is { } knownLine && column is { } knownColumn)
            return new SourceLocation(offset, knownLine, knownColumn, sourceName);

        // A generated offset must never be interpreted against the SQL SourceText.
        return new SourceLocation(offset, -1, -1, sourceName);
    }

    private static DiagnosticSeverity MapSeverity(TargetDiagnosticSeverity severity)
    {
        return severity switch
        {
            TargetDiagnosticSeverity.Hidden => DiagnosticSeverity.Hint,
            TargetDiagnosticSeverity.Info => DiagnosticSeverity.Info,
            TargetDiagnosticSeverity.Warning => DiagnosticSeverity.Warning,
            TargetDiagnosticSeverity.Error => DiagnosticSeverity.Error,
            _ => DiagnosticSeverity.Error
        };
    }
}
