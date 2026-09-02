using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static class EnumErrorMetadataCatalog
{
    public static IEnumerable<ErrorMetadata> Build()
    {
        yield return Entry(
            DiagnosticCode.MQ2042_InvalidEnumDeclaration,
            "A query-local enum declaration is missing a required name, backing type, delimiter, or member separator.",
            [
                "Use: enum Name : int { Member = 1 };.",
                "Separate members with commas and close the declaration with a right brace."
            ],
            "Core Spec - Enum Declarations");

        yield return Entry(
            DiagnosticCode.MQ2043_InvalidEnumBackingType,
            "Query-local enums require one of the eight CLR integral backing types supported by Musoq.",
            ["Use byte, sbyte, short, ushort, int, uint, long, or ulong."],
            "Core Spec - Enum Declarations");

        yield return Entry(
            DiagnosticCode.MQ2044_MissingEnumMemberValue,
            "Every enum member must have an explicit integral literal value; implicit numbering is intentionally unsupported.",
            ["Add '= <integral literal>' after the member name."],
            "Core Spec - Enum Declarations");

        yield return Entry(
            DiagnosticCode.MQ2045_DuplicateEnumMember,
            "Enum member names must be unique, including names that differ only by casing.",
            ["Rename or remove the duplicate member."],
            "Core Spec - Enum Declarations");

        yield return Entry(
            DiagnosticCode.MQ2046_EnumMemberValueOutOfRange,
            "The explicit member value cannot be represented by the enum's declared integral backing type.",
            [
                "Choose a value within the declared backing type's range.",
                "Use a wider backing type when the value is intentional."
            ],
            "Core Spec - Enum Declarations");

        yield return Entry(
            DiagnosticCode.MQ2047_EmptyEnumDeclaration,
            "An enum declaration must contain at least one explicitly-valued member.",
            ["Add a member such as None = 0."],
            "Core Spec - Enum Declarations");

        yield return Entry(
            DiagnosticCode.MQ2048_UnsupportedEnumSyntax,
            "The declaration uses a C#, PostgreSQL, or MySQL enum form that is not part of Musoq SQL.",
            ["Use: enum Name : int { Member = 1 }; or flags enum Name : uint { None = 0ui };."],
            "Core Spec - Enum Declarations");
    }
}
