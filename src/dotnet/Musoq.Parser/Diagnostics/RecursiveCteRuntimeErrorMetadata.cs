using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static class RecursiveCteRuntimeErrorMetadata
{
    public static IEnumerable<ErrorMetadata> Build()
    {
        yield return Entry(
            DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded,
            "Recursive CTE execution exceeded the configured number of recursive-member evaluations.",
            [
                "Add a terminating predicate or use UNION/keyed UNION for cycle elimination.",
                "Increase the recursive iteration limit only when the traversal is intentionally deeper."
            ],
            "Core Spec - Recursive CTEs");

        yield return Entry(
            DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded,
            "Recursive CTE execution accepted more rows than the configured safety limit.",
            [
                "Use UNION or keyed UNION to reject repeated identities.",
                "Increase the recursive row limit only after checking the expected result size."
            ],
            "Core Spec - Recursive CTEs");

        yield return Entry(
            DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded,
            "Recursive CTE execution retained more invariant rows than the configured snapshot safety limit.",
            [
                "Reduce or filter invariant recursive-member sources before traversal.",
                "Increase the recursive snapshot row limit only after checking the expected invariant size."
            ],
            "Core Spec - Recursive CTEs");
    }
}
