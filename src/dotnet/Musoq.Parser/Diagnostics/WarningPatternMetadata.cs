using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static partial class WarningMetadataCatalog
{
    private static IEnumerable<ErrorMetadata> BuildPatternMetadata()
    {
        yield return Entry(
            DiagnosticCode.MQ5015_SuspiciousRegexEscape,
            "An ordinary string escape changes a regex token before the regex engine evaluates it. For example, \\b becomes a backspace character rather than a word boundary.",
            ["Use a raw string literal such as r'\\bword\\b' for regex syntax.", "Double the backslash when the ordinary-string form is intentional."],
            "Core Spec - Regular Expressions");

        yield return Entry(
            DiagnosticCode.MQ5016_GlobWildcardInLike,
            "The pattern looks like a filesystem glob, but SQL LIKE recognizes '%' and '_' rather than '*' and '?'.",
            ["Use '%' or '_' for SQL LIKE wildcards.", "Use RLIKE when the pattern is a regular expression."],
            "Core Spec - LIKE and RLIKE");
    }
}
