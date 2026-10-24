using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static class PagingErrorMetadata
{
    internal static IEnumerable<ErrorMetadata> Build()
    {
        yield return Entry(
            DiagnosticCode.MQ3118_InvalidPagingCount,
            "SKIP and TAKE require an integral count in the supported range. Nullable values must contain a value.",
            ["Supply an integral count between 0 and 2147483647.", "Use a non-null paging value."],
            "Core Spec - TAKE and SKIP");
        yield return Entry(
            DiagnosticCode.MQ7019_InvalidRuntimePagingCount,
            "A runtime paging parameter is null, negative, or outside the supported 32-bit count range.",
            ["Supply an integral count between 0 and 2147483647.", "Use a non-null paging value."],
            "Core Spec - TAKE and SKIP");
    }
}
