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
    }
}
