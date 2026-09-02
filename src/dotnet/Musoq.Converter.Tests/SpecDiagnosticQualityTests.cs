using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class SpecDiagnosticQualityTests
{
    [TestMethod]
    public void WindowRestrictions_ReportActionableSourceSpan()
    {
        var cases = new[]
        {
            ("select RowNumber() over () from #system.dual()", DiagnosticCode.MQ3099_WindowOrderByRequired),
            ("select Lag(Dummy) over () from #system.dual()", DiagnosticCode.MQ3099_WindowOrderByRequired),
            ("select Count(*) from #system.dual() group by Dummy having RowNumber() over (order by Dummy) > 0", DiagnosticCode.MQ3101_WindowFunctionInFilter),
            ("select Dummy from #system.dual() where RowNumber() over (order by Dummy) > 1", DiagnosticCode.MQ3101_WindowFunctionInFilter)
        };

        foreach (var (query, expectedCode) in cases)
        {
            var diagnostic = Compile(query).Errors.Single();

            Assert.AreEqual(expectedCode, diagnostic.Code, Describe(diagnostic));
            Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase, Describe(diagnostic));
            Assert.IsTrue(diagnostic.Span.Start >= 0 && diagnostic.Span.Length > 0, Describe(diagnostic));
        }
    }

    [TestMethod]
    public void RecursiveCteWindowFunction_ReportsTheUnsupportedExpression()
    {
        const string query = "with recursive r as (select 1 as N from #system.dual() union all select RowNumber() over (order by N) as N from r) select N from r";
        var diagnostic = Compile(query).Errors.Single();

        Assert.AreEqual(DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, diagnostic.Code, Describe(diagnostic));
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase, Describe(diagnostic));
        var windowStart = query.IndexOf("RowNumber", StringComparison.Ordinal);
        Assert.AreEqual(windowStart, diagnostic.Span.Start, Describe(diagnostic));
        Assert.IsTrue(diagnostic.Span.Length > 0, Describe(diagnostic));
        StringAssert.Contains(diagnostic.Message, "window function", Describe(diagnostic));
    }

    [TestMethod]
    public void TableAndCoupleRestrictions_ReportActionableSourceSpan()
    {
        var cases = new[]
        {
            "table Empty { }",
            "table T { Name: string, Name: int }",
            "table T { Name: string }; table T { Age: int }; select 1 from #system.dual()",
            "couple raw.files with table Unknown as Y",
            "table T { Path: string }; couple raw.files() with table T as R",
            "select Dummy from #system.dual(); couple raw.files with settings prod as R"
        };

        foreach (var query in cases)
        {
            var result = Compile(query);
            Assert.IsTrue(result.Errors.Count > 0, $"Expected an error for: {query}");

            foreach (var diagnostic in result.Errors)
            {
                Assert.IsTrue(diagnostic.Span.Start >= 0 && diagnostic.Span.Length > 0, Describe(diagnostic));
                Assert.IsFalse(diagnostic.Message.StartsWith("The compiler encountered an internal failure", StringComparison.Ordinal), Describe(diagnostic));
            }
        }
    }

    [TestMethod]
    public void CollectionParameterInValues_ReportsTheParameterReference()
    {
        const string query = "param(xs: int[]) select * from values { { K: $xs } } v";
        var diagnostic = Compile(query).Errors.Single();

        Assert.AreEqual(DiagnosticCode.MQ3055_InvalidValuesSource, diagnostic.Code, Describe(diagnostic));
        Assert.AreEqual(query.IndexOf("$xs", StringComparison.Ordinal), diagnostic.Span.Start, Describe(diagnostic));
        Assert.AreEqual(3, diagnostic.Span.Length, Describe(diagnostic));
        StringAssert.Contains(diagnostic.Message, "collection parameter", Describe(diagnostic));
    }

    [TestMethod]
    public void EmptyInList_ReportsTheClosingParenthesis()
    {
        const string query = "select Dummy from #system.dual() where Dummy in ()";
        var diagnostic = Compile(query).Errors.Single();

        Assert.AreEqual(DiagnosticCode.MQ2037_EmptyPredicateListNotAllowed, diagnostic.Code, Describe(diagnostic));
        Assert.AreEqual(query.Length - 1, diagnostic.Span.Start, Describe(diagnostic));
        Assert.AreEqual(1, diagnostic.Span.Length, Describe(diagnostic));
    }

    [TestMethod]
    public void RenameDuplicateTarget_ReportsTheConflictingOutputName()
    {
        const string query = "select * rename (A as B) from values { { A: 1, B: 2 } } v";
        var diagnostic = Compile(query).Errors.Single();

        Assert.AreEqual(DiagnosticCode.MQ3069_StarRenameDuplicateTarget, diagnostic.Code, Describe(diagnostic));
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase, Describe(diagnostic));
        Assert.IsTrue(diagnostic.Span.Start >= 0 && diagnostic.Span.Length > 0, Describe(diagnostic));
        StringAssert.Contains(diagnostic.Message, "duplicate", Describe(diagnostic));
    }

    [TestMethod]
    public void UnknownCoupledSource_ReportsActionableSourceSpan()
    {
        const string query =
            "table CsvRow { Value: string }; couple separatedvalues.comma with table CsvRow as Csv; " +
            "select * from Csv()";
        var diagnostic = Compile(query).Errors.Single();

        Assert.AreEqual(DiagnosticCode.MQ3085_UnknownSource, diagnostic.Code, Describe(diagnostic));
        Assert.IsTrue(diagnostic.Span.Start >= 0 && diagnostic.Span.Length > 0, Describe(diagnostic));
        StringAssert.Contains(diagnostic.Message, "comma", Describe(diagnostic));
        StringAssert.Contains(diagnostic.Message, "schema", Describe(diagnostic));
    }

    [TestMethod]
    public void DeclarationOnlyBatch_ReportsMissingExecutableStatement()
    {
        const string query = "table T { Path: string }; couple #raw.files with table T as R";
        var diagnostic = Compile(query).Errors.Single();

        Assert.AreEqual(DiagnosticCode.MQ2016_IncompleteStatement, diagnostic.Code, Describe(diagnostic));
        Assert.AreEqual(query.IndexOf("couple", StringComparison.Ordinal), diagnostic.Span.Start, Describe(diagnostic));
        Assert.IsTrue(diagnostic.Span.Length > 0, Describe(diagnostic));
        StringAssert.Contains(diagnostic.Message, "no executable statement", Describe(diagnostic));
    }

    [TestMethod]
    public void CorrelatedSubqueryFallbacks_ReportActionableSourceSpan()
    {
        var cases = new[]
        {
            ("select a.K in (select b.K from values { { K: 1 } } b where b.K > a.K) from values { { K: 1 } } a", "correlated subquery strategy"),
            ("select a.K > any (select b.K from values { { K: 1 } } b where b.K = a.K) from values { { K: 1 } } a", "correlated quantified subquery strategy")
        };

        foreach (var (query, operation) in cases)
        {
            var diagnostic = Compile(query).Errors.Single();

            Assert.AreEqual(DiagnosticCode.MQ2024_InvalidSubquery, diagnostic.Code, Describe(diagnostic));
            Assert.IsTrue(diagnostic.Span.Start >= 0 && diagnostic.Span.Length > 0, Describe(diagnostic));
            StringAssert.Contains(diagnostic.Message, operation, Describe(diagnostic));
        }
    }

    private static BuildResult Compile(string query) => InstanceCreator.CompileWithDiagnostics(
        query,
        $"SpecDiagnosticQuality_{Guid.NewGuid():N}",
        new SystemSchemaProvider(),
        new TestsLoggerResolver());

    private static string Describe(Diagnostic diagnostic) =>
        $"[{diagnostic.Code}] {diagnostic.Phase} {diagnostic.Span}: {diagnostic.Message}";
}
