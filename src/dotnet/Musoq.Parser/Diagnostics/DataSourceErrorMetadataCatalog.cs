using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static class DataSourceErrorMetadataCatalog
{
    public static IEnumerable<ErrorMetadata> Build()
    {
        yield return Entry(
            DiagnosticCode.MQ7020_ReportedDataSourceFailure,
            "The datasource reported a recognized failure. Its provider code, category and correction guidance describe the problem.",
            ["Follow the provider's suggested fixes before retrying."],
            "Core Spec - Data Sources"
        );
        yield return Entry(
            DiagnosticCode.MQ5027_ReportedDataSourceWarning,
            "The datasource reported a non-fatal issue; query execution continues.",
            ["Review the provider's warning and suggested fixes."],
            "Core Spec - Data Sources"
        );
    }
}
