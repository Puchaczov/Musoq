using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class WarningMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildSourceContractMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ5013_SourceContractWarning,
            "A data source reported a non-fatal issue with the table contract or read modifiers.",
            [
                "Inspect the source contract warning message for the affected column or modifier.",
                "Remove unsupported modifiers when the data source should use its defaults."
            ],
            "Table/Couple Spec - Source Contract Diagnostics");

        yield return Entry(
            DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape,
            "A recognized escape sequence changes the value of an ordinary string that looks like a path. Existing ordinary-string escape semantics remain unchanged; this warning is advisory.",
            [
                "Use a raw literal such as r'path\\segment' when backslashes should remain literal.",
                "Double each intended backslash in an ordinary string, such as 'path\\\\segment'."
            ],
            "Core Spec - String Literals");
    }
}
