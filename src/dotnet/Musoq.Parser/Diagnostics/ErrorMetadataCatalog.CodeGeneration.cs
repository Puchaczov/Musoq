using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static class CodeGenerationErrorMetadataCatalog
{
    public static IEnumerable<ErrorMetadata> Build()
    {
        yield return Entry(DiagnosticCode.MQ8001_CodeGenerationFailed,
            "The Roslyn compilation of the internally generated C# code failed. This is typically an internal engine issue, not a user error.",
            ["Report the query to the Musoq issue tracker.", "Try simplifying the query to narrow down the trigger."],
            "Architecture - Code Generation");

        yield return Entry(DiagnosticCode.MQ8002_CompiledArtifactIncompatible,
            "The compiled query artifact cannot be used with the current script, schema, compilation options, engine version, or artifact loader.",
            ["Recompile the query artifact with the current engine and schema provider.", "Verify that the host cache key includes script, options, plugin/catalog versions, and schema compatibility inputs.", "If using a custom artifact type loader, ensure it returns the generated runnable type."], "Runtime V2 - Compiled Artifacts");
    }
}
