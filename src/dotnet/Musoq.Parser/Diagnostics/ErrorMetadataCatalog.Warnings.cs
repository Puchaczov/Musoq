using System.Collections.Generic;

namespace Musoq.Parser.Diagnostics;

internal static partial class WarningMetadataCatalog
{
    public static IEnumerable<ErrorMetadata> Build()
    {
        foreach (var metadata in BuildCoreWarningMetadata())
            yield return metadata;

        foreach (var metadata in BuildPatternMetadata())
            yield return metadata;

        foreach (var metadata in BuildPredicateMetadata())
            yield return metadata;

        foreach (var metadata in BuildOuterJoinMetadata())
            yield return metadata;

        foreach (var metadata in BuildDeclarationMetadata())
            yield return metadata;

        foreach (var metadata in BuildSourceContractMetadata())
            yield return metadata;

        foreach (var metadata in BuildOrderingMetadata())
            yield return metadata;
    }
}
