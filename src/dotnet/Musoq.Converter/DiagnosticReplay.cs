using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter;

internal static class DiagnosticReplay
{
    internal static void AddMissing(
        DiagnosticContext context,
        IEnumerable<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var existing = context.Diagnostics
            .Select(CreateKey)
            .ToHashSet();
        foreach (var diagnostic in diagnostics)
        {
            if (existing.Add(CreateKey(diagnostic)))
                context.AddRange([diagnostic]);
        }
    }

    private static DiagnosticKey CreateKey(Diagnostic diagnostic)
    {
        return new DiagnosticKey(
            diagnostic.Code,
            diagnostic.Severity,
            diagnostic.Message,
            diagnostic.Location.Offset,
            diagnostic.EndLocation.Offset);
    }

    private readonly record struct DiagnosticKey(
        DiagnosticCode Code,
        DiagnosticSeverity Severity,
        string Message,
        int Start,
        int End);
}
