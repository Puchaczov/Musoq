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

        context.AddRange(targetDiagnostics.Select(diagnostic => Convert(diagnostic, context.SourceText)));
    }

    private static Diagnostic Convert(TargetDiagnostic diagnostic, SourceText? sourceText)
    {
        var (start, end) = ResolveLocations(diagnostic, sourceText);
        return new Diagnostic(
            DiagnosticCode.MQ8001_CodeGenerationFailed,
            MapSeverity(diagnostic.Severity),
            $"[{diagnostic.Code}] {diagnostic.Message}",
            start,
            end,
            diagnostic.SourceSnippet);
    }

    private static (SourceLocation Start, SourceLocation End) ResolveLocations(
        TargetDiagnostic diagnostic,
        SourceText? sourceText)
    {
        if (sourceText is null)
            return (SourceLocation.None, SourceLocation.None);

        var startOffset = Math.Min(diagnostic.SourceRange?.Start ?? 0, sourceText.Length);
        var endOffset = Math.Min(diagnostic.SourceRange?.End ?? startOffset, sourceText.Length);
        var start = sourceText.GetLocation(startOffset).WithFilePath(diagnostic.SourceName ?? sourceText.FilePath);
        var end = sourceText.GetLocation(endOffset).WithFilePath(diagnostic.SourceName ?? sourceText.FilePath);
        return (start, end);
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
