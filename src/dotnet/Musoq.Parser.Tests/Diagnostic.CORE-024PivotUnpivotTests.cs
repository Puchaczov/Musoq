using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCore024PivotUnpivotTests
{
    [TestMethod]
    public void Pivot_MissingOn_ShouldReportExactDiagnostic()
    {
        const string query = "pivot #sales.orders() in ('Q1' as Q1) using Sum(Amount) as Sales";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2002_MissingToken,
            "PIVOT requires an ON clause after the source. Example: pivot #sales.orders() on Quarter in ('Q1' as Q1) using Sum(Amount) as Sales.",
            new TextSpan(query.IndexOf(" in ", StringComparison.Ordinal) + 1, 2),
            "Core Spec - Statement Structure");
    }

    [TestMethod]
    public void Pivot_MissingIn_ShouldReportExactDiagnostic()
    {
        const string query = "pivot #sales.orders() on Quarter using Sum(Amount) as Sales";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2002_MissingToken,
            "PIVOT ON requires a static IN (...) list so output columns are known at compile time.",
            SpanOf(query, "using"),
            "Core Spec - Statement Structure");
    }

    [TestMethod]
    public void Pivot_MissingUsing_ShouldReportExactDiagnostic()
    {
        const string query = "pivot #sales.orders() on Quarter in ('Q1' as Q1) group by Region";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2002_MissingToken,
            "PIVOT requires a USING clause with aggregate measures. Example: using Sum(Amount) as Sales.",
            SpanOf(query, "group by"),
            "Core Spec - Statement Structure");
    }

    [TestMethod]
    public void Pivot_EmptyIn_ShouldReportExactDiagnostic()
    {
        const string query = "pivot #sales.orders() on Quarter in () using Sum(Amount) as Sales";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2003_InvalidExpression,
            "PIVOT IN requires at least one constant value.",
            new TextSpan(query.IndexOf("in ()", StringComparison.Ordinal) + 4, 1),
            "Core Spec - Expressions");
    }

    [TestMethod]
    public void Pivot_TrailingInComma_ShouldReportExactDiagnostic()
    {
        const string query = "pivot #sales.orders() on Quarter in ('Q1' as Q1,) using Sum(Amount) as Sales";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2014_TrailingComma,
            "PIVOT IN list has a trailing comma. Add another value or remove the comma.",
            new TextSpan(query.IndexOf(",)", StringComparison.Ordinal) + 1, 1),
            "Core Spec - Lists");
    }

    [TestMethod]
    public void Pivot_NonConstantValue_ShouldReportExactDiagnostic()
    {
        const string query = "pivot #sales.orders() on Quarter in (Region as Region) using Sum(Amount) as Sales";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2003_InvalidExpression,
            "PIVOT IN values must be constants. Use literal values such as 'Q1', 2024, true, or null.",
            SpanOf(query, "Region"),
            "Core Spec - Expressions");
    }

    [TestMethod]
    public void Pivot_NonCallMeasure_ShouldReportExactDiagnostic()
    {
        const string query = "pivot #sales.orders() on Quarter in ('Q1' as Q1) using Amount as Sales";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2003_InvalidExpression,
            "PIVOT USING accepts aggregate function calls only. Use a form like USING Sum(Amount) as Sales.",
            SpanOf(query, "Amount"),
            "Core Spec - Expressions");
    }

    [TestMethod]
    public void Pivot_TupleArityMismatch_ShouldReportExactDiagnostic()
    {
        const string query = "pivot #sales.orders() on Year, Country in ((2000) as y2000) using Sum(Amount) as Sales";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2003_InvalidExpression,
            "PIVOT value tuple length mismatch. ON has 2 key(s), but this IN value has 1.",
            new TextSpan(query.IndexOf("2000", StringComparison.Ordinal), 4),
            "Core Spec - Expressions");
    }

    [TestMethod]
    public void Pivot_CaseInsensitiveGeneratedNameCollision_ShouldReportExactDiagnostic()
    {
        const string query = "pivot #sales.orders() on Quarter in ('Q1' as Sales, 'Q2' as sales) using Sum(Amount) as Total";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2008_DuplicateAlias,
            "PIVOT generated duplicate output column name 'sales'. Use unique pivot value aliases or measure aliases.",
            SpanOf(query, "Sum"),
            "Core Spec - Aliasing");
    }

    [TestMethod]
    public void Pivot_CombinedGeneratedNameCollision_ShouldReportExactDiagnostic()
    {
        const string query = "pivot #sales.orders() on Quarter in ('Q1' as A_B, 'Q2' as A) using Sum(Amount) as C, Count(*) as B_C";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2008_DuplicateAlias,
            "PIVOT generated duplicate output column name 'A_B_C'. Use unique pivot value aliases or measure aliases.",
            SpanOf(query, "Count"),
            "Core Spec - Aliasing");
    }

    [TestMethod]
    public void Unpivot_MissingOn_ShouldReportExactDiagnostic()
    {
        const string query = "unpivot #sales.wide() in (Q1 as Q1) using Sales";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2002_MissingToken,
            "UNPIVOT requires an ON clause after the source. Example: unpivot #sales.wide() on Quarter in (Q1 as Q1) using Sales.",
            SpanOf(query, "in"),
            "Core Spec - Statement Structure");
    }

    [TestMethod]
    public void Unpivot_MissingIn_ShouldReportExactDiagnostic()
    {
        const string query = "unpivot #sales.wide() on Quarter using Sales";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2002_MissingToken,
            "UNPIVOT ON requires an IN (...) list of value expressions.",
            SpanOf(query, "using"),
            "Core Spec - Statement Structure");
    }

    [TestMethod]
    public void Unpivot_MissingUsing_ShouldReportExactDiagnostic()
    {
        const string query = "unpivot #sales.wide() on Quarter in (Q1 as Q1) keep Region";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2002_MissingToken,
            "UNPIVOT requires a USING clause with the generated value column name. Example: using Sales.",
            SpanOf(query, "keep"),
            "Core Spec - Statement Structure");
    }

    [TestMethod]
    public void Unpivot_EmptyIn_ShouldReportExactDiagnostic()
    {
        const string query = "unpivot #sales.wide() on Quarter in () using Sales";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2003_InvalidExpression,
            "UNPIVOT IN requires at least one value expression.",
            new TextSpan(query.IndexOf("in ()", StringComparison.Ordinal) + 4, 1),
            "Core Spec - Expressions");
    }

    [TestMethod]
    public void Unpivot_TrailingInComma_ShouldReportExactDiagnostic()
    {
        const string query = "unpivot #sales.wide() on Quarter in (Q1 as Q1,) using Sales";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2014_TrailingComma,
            "UNPIVOT IN list has a trailing comma. Add another value expression or remove the comma.",
            new TextSpan(query.IndexOf(",)", StringComparison.Ordinal) + 1, 1),
            "Core Spec - Lists");
    }

    [TestMethod]
    public void Unpivot_ComplexEntryWithoutAlias_ShouldReportExactDiagnostic()
    {
        const string query = "unpivot #sales.wide() on Quarter in (Q1 + Q2) using Sales";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2022_InvalidAlias,
            "UNPIVOT IN entries require an alias unless the value expression is a simple identifier or property access.",
            SpanOf(query, "Q1 + Q2"),
            "Core Spec - Aliasing");
    }

    [TestMethod]
    public void Unpivot_ComplexKeepWithoutAlias_ShouldReportExactDiagnostic()
    {
        const string query = "unpivot #sales.wide() on Quarter in (Q1 as Q1) using Sales keep Region + ':' + Country";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2022_InvalidAlias,
            "UNPIVOT KEEP expressions require an alias unless the expression is a simple identifier or property access.",
            SpanOf(query, "Region + ':' + Country"),
            "Core Spec - Aliasing");
    }

    [TestMethod]
    public void Unpivot_DuplicateNameAndValueColumns_ShouldReportExactDiagnostic()
    {
        const string query = "unpivot #sales.wide() on Quarter in (Q1 as Q1) using Quarter";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2008_DuplicateAlias,
            "UNPIVOT generated duplicate output column name 'Quarter'. Use unique keep, name, and value column aliases.",
            new TextSpan(query.LastIndexOf("Quarter", StringComparison.Ordinal), "Quarter".Length),
            "Core Spec - Aliasing");
    }

    [TestMethod]
    public void Unpivot_DuplicateKeepColumns_ShouldReportExactDiagnostic()
    {
        const string query = "unpivot #sales.wide() on Quarter in (Q1 as Q1) using Sales keep Region as Keep, Country as keep";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2008_DuplicateAlias,
            "UNPIVOT generated duplicate output column name 'keep'. Use unique keep, name, and value column aliases.",
            SpanOf(query, "Country"),
            "Core Spec - Aliasing");
    }

    [TestMethod]
    public void Unpivot_CaseInsensitiveEntryNameCollision_ShouldReportExactDiagnostic()
    {
        const string query = "unpivot #sales.wide() on Quarter in (Q1 as Name, Q2 as name) using Sales";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2008_DuplicateAlias,
            "UNPIVOT IN generated duplicate name value 'name'. Use unique aliases in the IN list.",
            SpanOf(query, "name"),
            "Core Spec - Aliasing");
    }

    private static void AssertParseDiagnostic(
        string query,
        DiagnosticCode expectedCode,
        string expectedMessage,
        TextSpan expectedSpan,
        string expectedDocsReference)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        var result = new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(expectedMessage, diagnostic.Message);
        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.AreEqual(expectedDocsReference, diagnostic.DocsReference);
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    private static TextSpan SpanOf(string query, string text)
    {
        return new TextSpan(query.IndexOf(text, StringComparison.Ordinal), text.Length);
    }
}
