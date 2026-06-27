using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static class CodeGenerationErrorMetadataCatalog
{
    public static IEnumerable<ErrorMetadata> Build()
    {
        yield return Entry(
            DiagnosticCode.MQ8001_CodeGenerationFailed,
            "The Roslyn compilation of the internally generated C# code failed. This is typically an internal engine issue, not a user error.",
            [
                "Report the query to the Musoq issue tracker.",
                "Try simplifying the query to narrow down the trigger."
            ],
            "Architecture - Code Generation");
    }
}
