using System.Collections.Generic;
using System.Collections.Frozen;
using System.Linq;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Immutable registry containing the complete public definition of each
///     known diagnostic code. Message, severity, phase, guidance, and category
///     are resolved from this registry as one contract.
/// </summary>
internal static class DiagnosticDescriptorRegistry
{
    private static readonly FrozenDictionary<DiagnosticCode, DiagnosticDescriptor> Entries = BuildEntries();

    public static DiagnosticDescriptor? Get(DiagnosticCode code)
    {
        return Entries.GetValueOrDefault(code);
    }

    public static IReadOnlyCollection<DiagnosticDescriptor> All => Entries.Values;

    private static FrozenDictionary<DiagnosticCode, DiagnosticDescriptor> BuildEntries()
    {
        var entries = new Dictionary<DiagnosticCode, DiagnosticDescriptor>();

        foreach (var code in Enum.GetValues<DiagnosticCode>().Distinct())
        {
            if (!ErrorCatalog.HasTemplate(code) && ErrorMetadataCatalog.GetLegacy(code) == null)
                continue;

            var metadata = ErrorMetadataCatalog.GetLegacy(code);
            var suggestedFixes = metadata?.SuggestedFixes ?? [];
            var defaultActions = suggestedFixes
                .Select(static suggestion => DiagnosticAction.Suggestion(suggestion))
                .ToArray();

            entries.Add(code, new DiagnosticDescriptor(
                code,
                ErrorCatalog.GetTemplateLegacy(code),
                ErrorCatalog.GetDefaultSeverityLegacy(code),
                metadata?.Phase ?? DiagnosticPhaseMapping.FromCode(code),
                ErrorCatalog.GetCategoryLegacy(code),
                metadata?.Explanation,
                suggestedFixes,
                defaultActions,
                metadata?.DocsReference));
        }

        return entries.ToFrozenDictionary();
    }
}
