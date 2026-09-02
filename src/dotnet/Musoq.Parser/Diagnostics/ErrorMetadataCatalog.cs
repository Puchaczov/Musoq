using System.Collections.Frozen;
using System.Collections.Generic;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Catalog of rich error metadata (Why / Try / Docs) for diagnostic codes.
///     Provides actionable guidance for each known error.
/// </summary>
public static class ErrorMetadataCatalog
{
    private static readonly FrozenDictionary<DiagnosticCode, ErrorMetadata> Entries = BuildEntries();

    /// <summary>
    ///     Retrieves error metadata for a diagnostic code, or null if not found.
    /// </summary>
    public static ErrorMetadata? Get(DiagnosticCode code)
    {
        return DiagnosticDescriptorRegistry.Get(code)?.Metadata;
    }

    internal static IReadOnlyCollection<ErrorMetadata> All => Entries.Values;

    internal static ErrorMetadata? GetLegacy(DiagnosticCode code)
    {
        return Entries.GetValueOrDefault(code);
    }

    internal static ErrorMetadata Entry(
        DiagnosticCode code,
        string explanation,
        string[] suggestedFixes,
        string docsReference,
        DiagnosticPhase? phase = null)
    {
        return new ErrorMetadata(
            code,
            phase ?? DiagnosticPhaseMapping.FromCode(code),
            explanation,
            suggestedFixes,
            docsReference);
    }

    private static FrozenDictionary<DiagnosticCode, ErrorMetadata> BuildEntries()
    {
        var entries = new Dictionary<DiagnosticCode, ErrorMetadata>();

        AddRange(entries, LexerErrorMetadataCatalog.Build());
        AddRange(entries, ParserErrorMetadataCatalog.Build());
        AddRange(entries, EnumErrorMetadataCatalog.Build());
        AddRange(entries, SemanticErrorMetadataCatalog.Build());
        AddRange(entries, SchemaErrorMetadataCatalog.Build());
        AddRange(entries, WarningMetadataCatalog.Build());
        AddRange(entries, FeatureGateErrorMetadataCatalog.Build());
        AddRange(entries, RuntimeErrorMetadataCatalog.Build());
        AddRange(entries, CodeGenerationErrorMetadataCatalog.Build());
        AddRange(entries, InternalErrorMetadataCatalog.Build());

        return entries.ToFrozenDictionary();
    }

    private static void AddRange(Dictionary<DiagnosticCode, ErrorMetadata> entries, IEnumerable<ErrorMetadata> metadataEntries)
    {
        foreach (var metadata in metadataEntries)
        {
            if (!entries.TryAdd(metadata.Code, metadata))
                throw new InvalidOperationException($"Duplicate diagnostic metadata entry for {metadata.Code}.");
        }
    }
}
