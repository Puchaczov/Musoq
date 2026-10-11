using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class SemanticErrorMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildGeneratedExecutionMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ3084_SourceEntityRequiresRuntimeReflection,
            "Generated execution can access CLR members only when the source entity and its projected members are publicly referenceable. Private, object-typed, and custom runtime-dynamic entities would require reflection at query execution time.",
            [
                "Expose a public CLR entity contract with public instance members.",
                "Use native query-scoped rows with authoritative concrete column types.",
                "Resolve dynamic schemas before compilation and decode directly into typed carriers."
            ],
            "Generated Execution - Source Entity Contracts");
    }
}
