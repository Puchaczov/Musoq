using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core027SetOperationTests : BasicEntityTestBase
{
    [TestMethod]
    public void UnionAndUnionAll_ShouldDistinguishDuplicateRows()
    {
        const string union = "select Id, Name from #A.Entities() union select Id, Name from #B.Entities()";
        const string unionAll = "select Id, Name from #A.Entities() union all select Id, Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity("one") { Id = 1 },
                new BasicEntity("one") { Id = 1 },
                new BasicEntity("two") { Id = 2 }
            ],
            ["#B"] =
            [
                new BasicEntity("one") { Id = 1 },
                new BasicEntity("three") { Id = 3 }
            ]
        };

        var distinctTable = CreateAndRunVirtualMachine(union, sources).Run(TestContext.CancellationToken);
        var allTable = CreateAndRunVirtualMachine(unionAll, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            distinctTable,
            ("Id", typeof(int)),
            ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            distinctTable,
            [1, "one"],
            [2, "two"],
            [3, "three"]);
        TableMaterializationTestHelper.AssertRowsUnordered(
            allTable,
            [1, "one"],
            [1, "one"],
            [1, "one"],
            [2, "two"],
            [3, "three"]);
    }

    [TestMethod]
    public void ExceptAndIntersect_ShouldApplyEffectiveKeysToEveryLeftRow()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity("first") { Id = 1 },
                new BasicEntity("second") { Id = 1 },
                new BasicEntity("removed") { Id = 2 }
            ],
            ["#B"] = [new BasicEntity("right") { Id = 2 }],
            ["#C"] = [new BasicEntity("right") { Id = 1 }]
        };

        var except = CreateAndRunVirtualMachine(
                "select Id, Name from #A.Entities() except (Id) select Id, Name from #B.Entities()",
                sources)
            .Run(TestContext.CancellationToken);
        var intersect = CreateAndRunVirtualMachine(
                "select Id, Name from #A.Entities() intersect (Id) select Id, Name from #C.Entities()",
                sources)
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            except,
            [1, "first"],
            [1, "second"]);
        TableMaterializationTestHelper.AssertRowsUnordered(
            intersect,
            [1, "first"],
            [1, "second"]);
    }

    [TestMethod]
    public void ExplicitKeys_ShouldUseLeftProjectionNamesAndOrdinalPositions()
    {
        const string query = "select Id, Name as Label from #A.Entities() union (Id) select Id as OtherId, City as OtherLabel from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("left") { Id = 7 }],
            ["#B"] = [new BasicEntity { Id = 7, City = "right" }]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Id", typeof(int)),
            ("Label", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [7, "left"]);
    }

    [TestMethod]
    public void MixedSetOperatorChain_ShouldBeEvaluatedLeftAssociatively()
    {
        const string query = "select 1 as Id from #A.Entities() union select 2 as Id from #B.Entities() except select 1 as Id from #C.Entities() intersect select 2 as Id from #D.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("a")],
            ["#B"] = [new BasicEntity("b")],
            ["#C"] = [new BasicEntity("c")],
            ["#D"] = [new BasicEntity("d")]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2]);
    }

    [TestMethod]
    public void SetResultOrderAndPaging_ShouldApplyOnceAfterTheCompleteSet()
    {
        const string query = "select 1 as Value from #A.Entities() union all select 3 as Value from #B.Entities() union all select 2 as Value from #C.Entities() order by Value desc skip 1 take 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("a")],
            ["#B"] = [new BasicEntity("b")],
            ["#C"] = [new BasicEntity("c")]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [2], [1]);
    }

    [TestMethod]
    public void SetOperatorInCte_ShouldPreserveTheFirstProjectionShape()
    {
        const string query = "with combined as (select Id, Name as Label from #A.Entities() union all (Id) select Id as OtherId, City as OtherLabel from #B.Entities()) select Id, Label from combined order by Id";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("left") { Id = 1 }],
            ["#B"] = [new BasicEntity { Id = 2, City = "right" }]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Id", typeof(int)),
            ("Label", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "left"], [2, "right"]);
    }

    [TestMethod]
    public void SetOperator_WidthMismatch_ShouldReportExactDiagnostic()
    {
        const string query = "select Name from #A.Entities() union select Name, City from #B.Entities()";
        var diagnostic = AssertSingleError(Analyze(query), DiagnosticCode.MQ3019_SetOperatorColumnCount);

        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(
            "Set operator must have the same quantity of columns in both queries",
            diagnostic.Message);
        Assert.AreEqual(SpanOf(query, "Name, City"), diagnostic.Span);
        AssertDiagnosticEnvelope(diagnostic, query, "Core Spec - Set Operators");
    }

    [TestMethod]
    public void SetOperator_TypeMismatch_ShouldReportExpressionsRatherThanNodeTypes()
    {
        const string query = "select Name from #A.Entities() union select 1 as Name from #B.Entities()";
        var diagnostic = AssertSingleError(Analyze(query), DiagnosticCode.MQ3020_SetOperatorColumnTypes);

        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(
            "Set operator must have the same types of columns in both queries. Left column expression is Name and right column expression is 1",
            diagnostic.Message);
        Assert.AreEqual(SpanOf(query, "1"), diagnostic.Span);
        AssertDiagnosticEnvelope(diagnostic, query, "Core Spec - Set Operators");
    }

    [TestMethod]
    public void SetOperator_UnknownExplicitKey_ShouldPointAtTheKeyToken()
    {
        const string query = "select Name from #A.Entities() union (Missing) select Name from #B.Entities()";
        var diagnostic = AssertSingleError(Analyze(query), DiagnosticCode.MQ3001_UnknownColumn);

        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual("Unknown column 'Missing'.", diagnostic.Message);
        Assert.AreEqual(SpanOf(query, "Missing"), diagnostic.Span);
        AssertDiagnosticEnvelope(diagnostic, query, "Core Spec - Column References");
    }

    [TestMethod]
    public void SetOperator_ResultOrderBy_ShouldUseOnlyTheFirstOperandNames()
    {
        const string query = "select Name as First from #A.Entities() union all select Name as Second from #B.Entities() order by Second";
        var diagnostic = AssertSingleError(Analyze(query), DiagnosticCode.MQ3001_UnknownColumn);

        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual("Unknown column 'Second'.", diagnostic.Message);
        Assert.AreEqual(
            new TextSpan(query.LastIndexOf("Second", StringComparison.Ordinal), "Second".Length),
            diagnostic.Span);
        AssertDiagnosticEnvelope(diagnostic, query, "Core Spec - Column References");
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [],
                ["#B"] = [],
                ["#C"] = [],
                ["#D"] = []
            })).Analyze(query);
    }

    private static Diagnostic AssertSingleError(QueryAnalysisResult result, DiagnosticCode expectedCode)
    {
        Assert.IsFalse(result.IsSuccess, FormatDiagnostics(result));
        var errors = result.Errors.ToArray();
        Assert.HasCount(1, errors, FormatDiagnostics(result));
        Assert.AreEqual(expectedCode, errors[0].Code);
        return errors[0];
    }

    private static void AssertDiagnosticEnvelope(
        Diagnostic diagnostic,
        string query,
        string expectedDocsReference)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(diagnostic.Code, envelope.Code);
        Assert.AreEqual(expectedDocsReference, envelope.DocsReference);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.HasCount(envelope.SuggestedFixes.Count, envelope.Actions);
        Assert.IsTrue(envelope.Actions.All(static action =>
            action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));
    }

    private static TextSpan SpanOf(string query, string text)
    {
        return new TextSpan(query.IndexOf(text, StringComparison.Ordinal), text.Length);
    }

    private static string FormatDiagnostics(QueryAnalysisResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(static diagnostic =>
            $"{diagnostic.Code}: {diagnostic.Message} at {diagnostic.Span}"));
    }
}
