using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static class FeatureGateErrorMetadataCatalog
{
    public static IEnumerable<ErrorMetadata> Build()
    {
        yield return Entry(
            DiagnosticCode.MQ6001_CteUnavailable,
            "CTE syntax (WITH ... AS ...) is currently unavailable in this parser path.",
            [
                "Rewrite as a single SELECT or GROUP BY query.",
                "Use a source-level workaround such as a staged file or query."
            ],
            "Core Spec - CTE Availability");

        yield return Entry(
            DiagnosticCode.MQ6002_DescUnavailable,
            "DESC introspection is unavailable in this build due to alias-validator conflict.",
            ["Use schema probing workaround: SELECT * FROM #source(...) s TAKE 1."],
            "CLI Reference - DESC");

        yield return Entry(
            DiagnosticCode.MQ6003_SimpleCaseNotSupported,
            "Simple CASE syntax (CASE expr WHEN value ...) is not supported. Musoq supports searched CASE only.",
            ["Rewrite as: CASE WHEN expr = value THEN result ELSE default END."],
            "Core Spec - CASE Expressions");

        yield return Entry(
            DiagnosticCode.MQ6004_CoalesceWithLiteralNull,
            "Coalesce or IfNull with a literal NULL argument is not supported in this version.",
            ["Use: CASE WHEN x IS NULL THEN 'fallback' ELSE x END."],
            "Core Spec - Functions");
    }
}
