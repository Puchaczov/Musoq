using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class KeywordCollisionDiagnosticTests
{
    [TestMethod]
    public void ReservedKeywordsInExpressionPosition_ShouldReportOneTypedDiagnostic()
    {
        var failures = new List<string>();

        foreach (var keyword in KeywordCollisionCatalog.ReservedSqlIdentifiers)
        {
            var result = ParseWithDiagnostics($"select Name + {keyword} from #some.files()");

            if (result.Diagnostics.Count != 1)
            {
                failures.Add($"{keyword}: expected one diagnostic, got {result.Diagnostics.Count} ({result.FormatDiagnostics()})");
                continue;
            }

            var expectedCode = IsMissingOperandBoundary(keyword)
                ? DiagnosticCode.MQ2020_MissingOperand
                : DiagnosticCode.MQ2001_UnexpectedToken;
            if (result.Diagnostics[0].Code != expectedCode)
                failures.Add($"{keyword}: expected {expectedCode}, got {result.Diagnostics[0].Code}");
        }

        Assert.IsEmpty(failures, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public void ReservedKeywordFailure_ShouldRecoverOnlyAtStatementBoundary()
    {
        var failures = new List<string>();

        foreach (var keyword in KeywordCollisionCatalog.ReservedSqlIdentifiers)
        {
            var result = ParseWithDiagnostics(
                $"select Name + {keyword} from #some.files(); select Name from #some.files()");

            var expectedCode = IsMissingOperandBoundary(keyword)
                ? DiagnosticCode.MQ2020_MissingOperand
                : DiagnosticCode.MQ2001_UnexpectedToken;
            if (result.Diagnostics.Count != 1 || result.Diagnostics[0].Code != expectedCode)
            {
                failures.Add($"{keyword}: unexpected diagnostics ({result.FormatDiagnostics()})");
                continue;
            }

            if (result.Root == null || ((StatementsArrayNode)result.Root.Expression).Statements.Count() != 1)
                failures.Add($"{keyword}: recovery did not retain the valid following statement.");
        }

        Assert.IsEmpty(failures, string.Join(Environment.NewLine, failures));
    }

    private static bool IsMissingOperandBoundary(string keyword)
    {
        return keyword is "from" or "where" or "group" or "having" or "order" or "take" or "skip"
            or "and" or "or" or "as" or "union" or "except" or "intersect" or "on" or "when"
            or "then" or "else" or "end" or "window";
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var diagnostics = new DiagnosticBag();
        var parser = new Parser(new Lexer(query, true), diagnostics);
        return parser.ParseWithDiagnostics();
    }
}
