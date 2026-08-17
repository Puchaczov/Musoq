using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace Musoq.Targets.CSharpClr;

internal sealed record CSharpClrFinalizationResult(
    EmitResult EmitResult,
    ExecutableQueryArtifact? Artifact) : TargetFinalizationResult(
        ExecutionTargetIds.CSharpClr,
        EmitResult.Success,
        CreateDiagnostics(EmitResult),
        Artifact)
{
    private static IReadOnlyList<TargetDiagnostic> CreateDiagnostics(EmitResult result)
    {
        var diagnostics = new TargetDiagnostic[result.Diagnostics.Length];
        for (var index = 0; index < result.Diagnostics.Length; index++)
        {
            var diagnostic = result.Diagnostics[index];
            diagnostics[index] = new TargetDiagnostic(
                diagnostic.Id,
                MapSeverity(diagnostic.Severity),
                diagnostic.ToString(),
                CreateSourceRange(diagnostic),
                diagnostic.Location.SourceTree?.FilePath,
                CreateDiagnosticSourceSnippet(diagnostic));
        }

        return diagnostics;
    }

    private static TargetDiagnosticSeverity MapSeverity(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Hidden => TargetDiagnosticSeverity.Hidden,
            DiagnosticSeverity.Info => TargetDiagnosticSeverity.Info,
            DiagnosticSeverity.Warning => TargetDiagnosticSeverity.Warning,
            DiagnosticSeverity.Error => TargetDiagnosticSeverity.Error,
            _ => TargetDiagnosticSeverity.Error
        };
    }

    private static TargetSourceRange? CreateSourceRange(Diagnostic diagnostic)
    {
        if (!diagnostic.Location.IsInSource)
            return null;

        var span = diagnostic.Location.SourceSpan;
        var lineSpan = diagnostic.Location.GetLineSpan();
        return new TargetSourceRange(
            span.Start,
            span.Length,
            lineSpan.StartLinePosition.Line + 1,
            lineSpan.StartLinePosition.Character + 1,
            lineSpan.EndLinePosition.Line + 1,
            lineSpan.EndLinePosition.Character + 1);
    }

    private static string? CreateDiagnosticSourceSnippet(Diagnostic diagnostic)
    {
        if (!diagnostic.Location.IsInSource)
            return null;

        var sourceTree = diagnostic.Location.SourceTree;
        if (sourceTree is null)
            return null;

        var sourceText = sourceTree.GetText();
        var lineSpan = diagnostic.Location.GetLineSpan();
        var lineIndex = lineSpan.StartLinePosition.Line;

        if (lineIndex < 0 || lineIndex >= sourceText.Lines.Count)
            return null;

        var builder = new StringBuilder();
        var start = Math.Max(0, lineIndex - 1);
        var end = Math.Min(sourceText.Lines.Count - 1, lineIndex + 1);

        builder.AppendLine("-- source snippet --");

        for (var index = start; index <= end; index++)
        {
            var textLine = sourceText.Lines[index].ToString().TrimEnd();
            builder.Append(index + 1);
            builder.Append(": ");
            builder.AppendLine(textLine);
        }

        var caretColumn = Math.Max(0, lineSpan.StartLinePosition.Character);
        builder.Append("   ");
        builder.Append(' ', Math.Min(caretColumn, 120));
        builder.AppendLine("^");
        builder.Append("-- end snippet --");

        return builder.ToString();
    }
}
