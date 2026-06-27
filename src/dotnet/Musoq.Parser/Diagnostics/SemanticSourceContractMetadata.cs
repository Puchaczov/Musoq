using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class SemanticErrorMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildSourceContractMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ3071_SourceContractError,
            "A data source reported that the table contract cannot be honored.",
            [
                "Adjust the annotated table column type or read modifier to a value supported by the data source.",
                "Remove the column modifier when the data source should use its own default behavior."
            ],
            "Table/Couple Spec - Source Contract Diagnostics");
    }
}
