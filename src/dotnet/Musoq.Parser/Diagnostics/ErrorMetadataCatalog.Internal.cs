using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static class InternalErrorMetadataCatalog
{
    public static IEnumerable<ErrorMetadata> Build()
    {
        yield return Entry(
            DiagnosticCode.MQ9999_Unknown,
            "An error was reported without a more specific diagnostic classification.",
            [
                "Check the surrounding diagnostic message for the concrete failure.",
                "Report the query if this generic diagnostic hides a repeatable engine issue."
            ],
            "Diagnostics - Unknown Errors");
    }
}
