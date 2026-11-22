using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class MirageBindingDiagnosticsTests
{
    [TestMethod]
    [DataRow("2.5 * Sqrt(4.0)", "Decimal", "Double", "double")]
    [DataRow("Sqrt(4.0) * 2.5", "Double", "Decimal", "double")]
    [DataRow("2.5 * ToFloat(4)", "Decimal", "Single", "float")]
    [DataRow("ToFloat(4) * 2.5", "Single", "Decimal", "float")]
    public void DecimalAndFloatingPointArithmetic_ShouldLocateExpressionAndSuggestACast(
        string expression,
        string leftType,
        string rightType,
        string castType
    )
    {
        var sql = $"select {expression} as Result from values {{ (Id: 1) }} v";

        var result = Analyze(sql);

        var error = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ3007_InvalidOperandTypes,
            sql
        );
        Assert.AreEqual("Multiply", error.Arguments["operator"]);
        Assert.AreEqual(leftType, error.Arguments["leftType"]);
        Assert.AreEqual(rightType, error.Arguments["rightType"]);
        Assert.AreEqual(sql.IndexOf(expression, StringComparison.Ordinal), error.Span.Start);
        StringAssert.Contains(error.SuggestedFixes.Single().Title, $"::{castType}");
        var correctedSql = sql.Replace("2.5", $"2.5::{castType}", StringComparison.Ordinal);
        DiagnosticContractTestAssertions.AssertNoErrors(Analyze(correctedSql), correctedSql);
    }

    [TestMethod]
    public void IncompatibleIntegralOperands_ShouldLocateExpressionAndDescribeOperands()
    {
        const string expression = "Wide::ulong * Smaller";
        const string sql = $"select {expression} as Result from values {{ (Wide: 1, Smaller: 2) }} v";

        var result = Analyze(sql);

        var error = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ3007_InvalidOperandTypes,
            sql
        );
        Assert.AreEqual("Multiply", error.Arguments["operator"]);
        Assert.AreEqual("UInt64", error.Arguments["leftType"]);
        Assert.AreEqual("Int32", error.Arguments["rightType"]);
        Assert.AreEqual(expression, sql.Substring(error.Span.Start, error.Span.Length));
    }

    [TestMethod]
    public void QualifiedOrderByProjectionAlias_ShouldSuggestRemovingTheQualifier()
    {
        const string sql = "select s.N + 1 as Total from values { (N: 2), (N: 1) } s order by s.Total";

        var result = Analyze(sql);

        var error = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCode.MQ3001_UnknownColumn);
        StringAssert.Contains(error.Message, "without a source qualifier in ORDER BY");
        Assert.IsFalse(Analyze(sql.Replace("s.Total", "Total")).HasErrors);
    }

    private static QueryAnalysisResult Analyze(string sql) =>
        new QueryAnalyzer(
            new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = [] })
        ).Analyze(sql);
}
