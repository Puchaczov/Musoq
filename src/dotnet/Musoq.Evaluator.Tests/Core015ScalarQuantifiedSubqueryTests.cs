using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core015ScalarQuantifiedSubqueryTests : BasicEntityTestBase
{
    [TestMethod]
    public void ScalarFromFirstQuery_ShouldReturnTheSingleValue()
    {
        const string query = """
            select a.City, (
                from #B.Entities() b
                where b.Country = 'FRANCE'
                select b.City
            ) as MatchCity
            from #A.Entities() a
            order by a.City
            """;

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("MatchCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["BERLIN", "PARIS"],
            ["PARIS", "PARIS"],
            ["WARSAW", "PARIS"]);
    }

    [TestMethod]
    public void ScalarValuesTakeOne_ShouldSuppressTheMultiRowBoundary()
    {
        const string query = """
            select (
                select Value
                from values { { Value: 3 }, { Value: 1 } } valuesSource
                order by Value desc
                take 1
            ) as TopValue
            from #A.Entities() a
            """;

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("TopValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            new object?[] { 3 },
            new object?[] { 3 },
            new object?[] { 3 });
    }

    [TestMethod]
    public void ScalarValuesWithTwoRows_ShouldReportExactCardinalityDiagnostic()
    {
        const string query = "select (select Value from values { { Value: 1 }, { Value: 2 } } valuesSource) as Value from #A.Entities() a";
        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources()));
        var envelope = exception.PrimaryEnvelope;
        var expectedStart = query.IndexOf("Value", query.IndexOf("select Value", StringComparison.Ordinal), StringComparison.Ordinal);
        var expectedEnd = expectedStart + "Value".Length - 1;

        Assert.AreEqual(DiagnosticCode.MQ3095_ScalarSubqueryCardinality, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedEnd - expectedStart + 1, envelope.Length);
        StringAssert.Contains(envelope.Message, "Scalar subquery may return more than one row");
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.AreEqual(
            "A scalar subquery must be provably single-row before its value can be used as a scalar.",
            envelope.Explanation);
        Assert.AreEqual("Core Spec - Subqueries", envelope.DocsReference);
        CollectionAssert.AreEqual(
            new[]
            {
                "Add an aggregate, TAKE 1, or another predicate that guarantees one row.",
                "Use IN or EXISTS when multiple rows are intended."
            },
            envelope.SuggestedFixes.ToArray());
        Assert.HasCount(2, envelope.Actions);
        Assert.IsTrue(envelope.Actions.All(static action =>
            action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));
        Assert.HasCount(0, envelope.Arguments);
    }

    [TestMethod]
    public void ScalarSubqueryRuntimeCardinalityViolation_ShouldUseInternalExecutionEnvelope()
    {
        const string query = "select (select b.City from #B.Entities() b where b.Country = 'POLAND') as City from #A.Entities() a";
        var vm = CreateAndRunVirtualMachine(query, CreateSources());

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
        var envelope = exception.Envelope ?? throw new AssertFailedException("Expected a structured runtime envelope.");

        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Internal, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Internal, envelope.SourceKind);
        Assert.IsNull(envelope.Offset);
        Assert.IsNull(envelope.Length);
        Assert.IsTrue(envelope.Arguments.ContainsKey("correlationId"));
        Assert.IsTrue(envelope.Arguments.ContainsKey("exceptionType"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.CorrelationId));
        Assert.IsNotNull(exception.InnerException);
    }

    [TestMethod]
    public void ScalarSubqueryWithMultipleColumns_ShouldReportExactInvalidSubqueryContract()
    {
        const string query = "select (select b.City, b.Country from #B.Entities() b) as Pair from #A.Entities() a";
        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources()));
        var envelope = exception.PrimaryEnvelope;
        var expectedStart = query.IndexOf("City", query.IndexOf("select b.City", StringComparison.Ordinal), StringComparison.Ordinal);
        var expectedEnd = query.IndexOf("Country", expectedStart, StringComparison.Ordinal) + "Country".Length - 1;

        Assert.AreEqual(DiagnosticCode.MQ2024_InvalidSubquery, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedEnd - expectedStart + 1, envelope.Length);
        StringAssert.Contains(envelope.Message, "Scalar subquery must return exactly one column.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.AreEqual(
            "A subquery is malformed or appears in a location where this parser path cannot accept it.",
            envelope.Explanation);
        Assert.AreEqual("Core Spec - Subqueries", envelope.DocsReference);
        AssertGenericSubqueryFixes(envelope);
    }

    [TestMethod]
    public void QuantifiedSubqueryWithMultipleColumns_ShouldReportExactParseContract()
    {
        const string query = "select a.City from #A.Entities() a where a.Population > any (select b.Population, b.City from #B.Entities() b)";
        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources()));
        var envelope = exception.PrimaryEnvelope;
        var expectedStart = query.IndexOf("Population", query.IndexOf("select b.Population", StringComparison.Ordinal), StringComparison.Ordinal);
        var expectedEnd = query.IndexOf("from #B", expectedStart, StringComparison.Ordinal) - 2;

        Assert.AreEqual(DiagnosticCode.MQ2024_InvalidSubquery, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedEnd - expectedStart + 1, envelope.Length);
        StringAssert.Contains(envelope.Message, "Quantified subquery must return exactly one column.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.AreEqual(
            "A subquery is malformed or appears in a location where this parser path cannot accept it.",
            envelope.Explanation);
        Assert.AreEqual("Core Spec - Subqueries", envelope.DocsReference);
        AssertGenericSubqueryFixes(envelope);
    }

    [TestMethod]
    [DataRow("ANY")]
    [DataRow("SOME")]
    public void EqualityQuantifiedSetOperator_ShouldUseTheInSubqueryPath(string quantifier)
    {
        var query = $"select a.City from #A.Entities() a where a.City = {quantifier} (select b.City from #B.Entities() b where b.City = 'WARSAW' union (City) select c.City from #C.Entities() c where c.City = 'PARIS') order by a.City";

        var table = CreateAndRunVirtualMachine(query, CreateSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["PARIS"], ["WARSAW"]);

    }

    [TestMethod]
    public void NonEqualityQuantifiedSetOperator_ShouldReportExactUnsupportedContract()
    {
        const string query = "select a.City from #A.Entities() a where a.Population > any (select b.Population from #B.Entities() b union (Population) select c.Population from #C.Entities() c)";
        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources()));
        var envelope = exception.PrimaryEnvelope;
        var expectedStart = query.IndexOf("Population", query.IndexOf("select b.Population", StringComparison.Ordinal), StringComparison.Ordinal);
        var expectedEnd = query.IndexOf("c.Population", expectedStart, StringComparison.Ordinal) + "c.Population".Length - 1;

        Assert.AreEqual(DiagnosticCode.MQ2024_InvalidSubquery, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedEnd - expectedStart + 1, envelope.Length);
        StringAssert.Contains(envelope.Message, "Quantified subqueries over set operators are not supported yet.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.AreEqual(
            "A subquery is malformed or appears in a location where this parser path cannot accept it.",
            envelope.Explanation);
        Assert.AreEqual("Core Spec - Subqueries", envelope.DocsReference);
        AssertGenericSubqueryFixes(envelope);
    }

    [TestMethod]
    public void CorrelatedScalarSetBranchesWithDifferentKeys_ShouldReportExactContract()
    {
        const string query = "select a.City, (select b.City from #B.Entities() b where b.Country = a.Country union (City) select c.City from #C.Entities() c where c.City = a.City) as MatchCity from #A.Entities() a";
        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources()));
        var envelope = exception.PrimaryEnvelope;
        var expectedStart = query.IndexOf("City", query.IndexOf("select b.City", StringComparison.Ordinal), StringComparison.Ordinal);
        var expectedEnd = query.LastIndexOf("a.City", StringComparison.Ordinal) + "a.City".Length - 1;

        Assert.AreEqual(DiagnosticCode.MQ2024_InvalidSubquery, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedEnd - expectedStart + 1, envelope.Length);
        StringAssert.Contains(envelope.Message, "Every correlated scalar set-operator branch must expose the same equality correlation key.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.AreEqual(
            "A subquery is malformed or appears in a location where this parser path cannot accept it.",
            envelope.Explanation);
        Assert.AreEqual("Core Spec - Subqueries", envelope.DocsReference);
        AssertGenericSubqueryFixes(envelope);
    }

    [TestMethod]
    public void NonEqualityQuantifiedProjectionReusingItsEqualityKey_ShouldReportExactFallbackContract()
    {
        const string query = "select a.Id, a.Id > any (select b.Id from #B.Entities() b where b.Id = a.Id) as HasGreater from #A.Entities() a";
        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources()));
        var envelope = exception.PrimaryEnvelope;
        var quantifiedExpressionStart = query.IndexOf("a.Id > any", StringComparison.Ordinal);
        var expectedStart = query.IndexOf("Id", quantifiedExpressionStart + 1, StringComparison.Ordinal);
        var expectedEnd = query.IndexOf(") as HasGreater", expectedStart, StringComparison.Ordinal);

        Assert.AreEqual(DiagnosticCode.MQ2024_InvalidSubquery, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedEnd - expectedStart + 1, envelope.Length);
        StringAssert.Contains(
            envelope.Message,
            "This non-equality quantified predicate reuses its equality correlation key in a value expression and cannot be lowered safely.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.AreEqual(
            "A subquery is malformed or appears in a location where this parser path cannot accept it.",
            envelope.Explanation);
        Assert.AreEqual("Core Spec - Subqueries", envelope.DocsReference);
        AssertGenericSubqueryFixes(envelope);
    }

    private static void AssertGenericSubqueryFixes(MusoqErrorEnvelope envelope)
    {
        CollectionAssert.AreEqual(
            new[]
            {
                "Ensure the subquery starts with SELECT and is enclosed in parentheses.",
                "Use the subquery only in a supported expression or source position."
            },
            envelope.SuggestedFixes.ToArray());
        Assert.HasCount(2, envelope.Actions);
        Assert.IsTrue(envelope.Actions.All(static action =>
            action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));
        Assert.HasCount(0, envelope.Arguments);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity("WARSAW", "POLAND", 500),
                new BasicEntity("BERLIN", "GERMANY", 250),
                new BasicEntity("PARIS", "FRANCE", 300)
            ],
            ["#B"] =
            [
                new BasicEntity("WARSAW", "POLAND", 100),
                new BasicEntity("KRAKOW", "POLAND", 110),
                new BasicEntity("PARIS", "FRANCE", 450)
            ],
            ["#C"] =
            [
                new BasicEntity("KRAKOW", "POLAND", 10),
                new BasicEntity("PARIS", "FRANCE", 20)
            ]
        };
    }
}
