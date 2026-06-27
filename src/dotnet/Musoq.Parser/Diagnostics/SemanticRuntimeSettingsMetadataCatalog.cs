using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class SemanticErrorMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildSourceRuntimeSettingsMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ3067_MissingSourceRuntimeSetting,
            "A data source requires a runtime setting that was not provided.",
            [
                "Provide the setting through the configured source runtime settings resolver.",
                "Use DESC SETTINGS to inspect required settings without revealing secret values."
            ],
            "Data Source Runtime Settings");
    }
}
