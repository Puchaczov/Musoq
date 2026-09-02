using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Musoq.Parser.Diagnostics;

internal static partial class SyntaxDiagnosticEnhancer
{
    private static readonly FrozenDictionary<string, DialectKeywordHelp> DialectKeywordHelpMap =
        new Dictionary<string, DialectKeywordHelp>(StringComparer.OrdinalIgnoreCase)
        {
            ["LIMIT"] = new(
                "Musoq uses TAKE instead of LIMIT.",
                [
                    "Replace LIMIT n with TAKE n.",
                    "Example: SELECT Name FROM #schema.method() alias TAKE 5"
                ],
                "Core Spec §TAKE / SKIP"),
            ["OFFSET"] = new(
                "Musoq uses SKIP instead of OFFSET.",
                [
                    "Replace OFFSET n with SKIP n.",
                    "If you need paging, use ORDER BY ... TAKE ... SKIP ..."
                ],
                "Core Spec §TAKE / SKIP"),
            ["TOP"] = new(
                "Musoq does not use TOP in the SELECT list. Use TAKE after the FROM clause instead.",
                [
                    "Rewrite SELECT TOP 5 ... as SELECT ... FROM ... TAKE 5.",
                    "Keep TAKE near the end of the query after FROM / ORDER BY."
                ],
                "Core Spec §TAKE / SKIP"),
            ["FIRST"] = new(
                "Musoq does not use FIRST in the SELECT list. Use TAKE after the FROM clause instead.",
                [
                    "Rewrite SELECT FIRST 5 ... as SELECT ... FROM ... TAKE 5.",
                    "Keep TAKE near the end of the query after FROM / ORDER BY."
                ],
                "Core Spec §TAKE / SKIP"),
            ["FETCH"] = new(
                "Musoq does not support SQL Server OFFSET/FETCH paging syntax. Use TAKE and SKIP instead.",
                [
                    "Replace OFFSET ... FETCH ... with TAKE ... SKIP ...",
                    "Example: SELECT ... ORDER BY Name TAKE 5 SKIP 3"
                ],
                "Core Spec §TAKE / SKIP"),
            ["ROWS"] = new(
                "Musoq does not support SQL Server OFFSET/FETCH ROWS syntax. Use TAKE and SKIP instead.",
                [
                    "Remove ROWS/ONLY keywords and rewrite with TAKE / SKIP.",
                    "Example: SELECT ... ORDER BY Name TAKE 5 SKIP 3"
                ],
                "Core Spec §TAKE / SKIP"),
            ["NEXT"] = new(
                "Musoq does not support SQL Server FETCH NEXT syntax. Use TAKE and SKIP instead.",
                [
                    "Rewrite FETCH NEXT n ROWS ONLY as TAKE n.",
                    "Combine with SKIP if you need offset paging."
                ],
                "Core Spec §TAKE / SKIP"),
            ["ONLY"] = new(
                "Musoq does not support SQL Server FETCH ... ONLY syntax. Use TAKE and SKIP instead.",
                [
                    "Remove ONLY and rewrite the paging clause with TAKE / SKIP.",
                    "Example: SELECT ... ORDER BY Name TAKE 5 SKIP 3"
                ],
                "Core Spec §TAKE / SKIP"),
            ["ILIKE"] = new(
                "Musoq uses LIKE for pattern matching. ILIKE (case-insensitive LIKE) is a PostgreSQL extension not supported in Musoq.",
                [
                    "Replace ILIKE with LIKE.",
                    "For case-insensitive matching, use: WHERE ToLower(Name) LIKE '%value%'"
                ],
                "Core Spec §LIKE Operator"),
            ["CAST"] = new(
                "Musoq uses strict postfix casts such as expression::Int32 instead of CAST(expression AS type).",
                [
                    "Rewrite CAST(value AS Type) as value::Type.",
                    "Use ToInt32(value) or another ToXxx helper when soft conversion is intended."
                ],
                "Core Spec §Strict Postfix Casts")
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
}
