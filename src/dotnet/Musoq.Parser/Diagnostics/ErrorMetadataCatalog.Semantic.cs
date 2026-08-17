using System.Collections.Generic;

namespace Musoq.Parser.Diagnostics;
internal static partial class SemanticErrorMetadataCatalog
{
    public static IEnumerable<ErrorMetadata> Build()
    {
        foreach (var metadata in BuildCoreMetadata()) yield return metadata;
        foreach (var metadata in BuildAsOfJoinMetadata()) yield return metadata;
        foreach (var metadata in BuildStarModifierMetadata()) yield return metadata;
        foreach (var metadata in BuildStarRenameMetadata()) yield return metadata;
        foreach (var metadata in BuildQueryFeatureMetadata()) yield return metadata;
        foreach (var metadata in BuildSourceRuntimeSettingsMetadata()) yield return metadata;
        foreach (var metadata in BuildSourceContractMetadata()) yield return metadata;
        foreach (var metadata in BuildGeneratedExecutionMetadata()) yield return metadata;
        foreach (var metadata in BuildRecursiveCteMetadata()) yield return metadata;
        foreach (var metadata in BuildNamedSourceArgumentMetadata()) yield return metadata;
        foreach (var metadata in BuildPrecisionMetadata()) yield return metadata;
    }
}
