using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class SemanticErrorMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildEnumMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ3106_DuplicateEnumType,
            "Query-local enum type names are case-insensitive and must be unique within a query batch.",
            [
                "Remove the duplicate enum declaration.",
                "Rename one declaration so its name differs by more than letter casing."
            ],
            "Core Spec - Enum Types");

        yield return Entry(
            DiagnosticCode.MQ3107_UnknownEnumType,
            "A TABLE enum type must be declared earlier in the query or identify a reachable native CLR enum exactly.",
            [
                "Move the query-local enum declaration before the TABLE statement.",
                "For a native enum, use its exact fully qualified CLR type name."
            ],
            "TABLE/COUPLE Spec - Enum Types");

        yield return Entry(DiagnosticCode.MQ3108_UnknownEnumMember,
            "Quoted enum member names use exact, case-sensitive matching.",
            ["Use a member name exactly as declared.", "Use a representable numeric source value only at a declared enum source boundary."],
            "Core Spec - Enum Types");
        yield return Entry(DiagnosticCode.MQ3109_EnumIdentityMismatch,
            "Enums are nominal types; equal backing types do not make two enum identities compatible.",
            ["Compare values from the same enum declaration.", "Project EnumValue(...) when an explicitly numeric result is required."],
            "Core Spec - Enum Types");
        yield return Entry(DiagnosticCode.MQ3110_UnsupportedEnumOperator,
            "Enums support equality, inequality, membership, null checks, and the explicit enum helpers only.",
            ["Use =, <>, IN, NOT IN, IS NULL, or IS NOT NULL.", "Use HasAnyFlags or HasAllFlags for flags masks."],
            "Core Spec - Enum Types");
        yield return Entry(DiagnosticCode.MQ3111_InvalidEnumHelper,
            "The enum helper name, enum kind, argument count, or literal member arguments are invalid.",
            ["Pass an enum expression as the first argument.", "Pass exact quoted member names to flags helpers."],
            "Core Spec - Enum Helpers");
        yield return Entry(DiagnosticCode.MQ3112_UnsupportedEnumScriptParameter,
            "Enum-valued and implicit string-to-enum script parameters are deferred in the first enum contract.",
            ["Use enum member literals directly in the query.", "Pass an ordinary primitive parameter to a separate non-enum expression."],
            "Core Spec - Enum Types");
        yield return Entry(DiagnosticCode.MQ3113_UnsupportedEnumOutputTarget,
            "Final enum projections are numeric and cannot bind directly to CLR enum DTO members in v1.",
            ["Use the enum backing integral type in the output DTO.", "Use EnumName(...) when textual output is intended."],
            "Core Spec - Enum Output");
        yield return Entry(DiagnosticCode.MQ3114_EnumSourceCapabilityRequired,
            "Dynamic enum columns require a source that advertises logical scalar reads.",
            ["Upgrade the datasource to the enum-capable source contract.", "Remove the enum TABLE column until the datasource supports logical scalar reads."],
            "TABLE/COUPLE Spec - Enum Sources");
        yield return Entry(DiagnosticCode.MQ3115_EnumDescriptorMismatch,
            "The source enum descriptor differs from the descriptor frozen when the query was compiled.",
            ["Recompile the query against the current datasource schema.", "Keep descriptor fingerprints stable for the lifetime of a compiled query."],
            "Core Spec - Enum Sources");
    }
}
