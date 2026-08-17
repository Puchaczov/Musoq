using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static class InternalErrorMetadataCatalog
{
    public static IEnumerable<ErrorMetadata> Build()
    {
        yield return Entry(
            DiagnosticCode.MQ9001_InternalCompilerError,
            "The compiler reached an invariant failure without a user-owned query classification. The diagnostic is intentionally safe and does not expose engine exception details.",
            [
                "Record the correlation identifier and the smallest query that reproduces the issue.",
                "Report the diagnostic with the query text and engine version."
            ],
            "Diagnostics - Internal Compiler Errors",
            DiagnosticPhase.Internal);

        yield return Entry(
            DiagnosticCode.MQ9002_InternalExecutionError,
            "The execution engine reached an invariant failure while evaluating a compiled query.",
            [
                "Record the correlation identifier and the query shape that triggered the failure.",
                "Report the diagnostic without including secret source arguments."
            ],
            "Diagnostics - Internal Execution Errors",
            DiagnosticPhase.Internal);

    }
}
