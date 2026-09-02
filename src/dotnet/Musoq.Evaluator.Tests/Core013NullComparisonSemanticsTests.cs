using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core013NullComparisonSemanticsTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhereOrdinaryComparisonWithNull_ShouldFilterEveryRowAsUnknown()
    {
        const string query = "select Name from #A.Entities() where Name = null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "value" }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void ProjectionOrdinaryComparisonWithNull_ShouldReturnUnknown()
    {
        const string query = "select Name = null as ComparisonResult from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "value" }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsNull(table[0].Values[0]);
        Assert.IsNull(table[1].Values[0]);
    }

    [TestMethod]
    public void ProjectionOrdinaryComparisonWithNullLeft_ShouldReturnUnknown()
    {
        const string query = "select Name = 'value' as EqualResult, Name <> 'value' as NotEqualResult from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "value" }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        CollectionAssert.AreEqual(new object?[] { null, null }, table[0].Values);
        CollectionAssert.AreEqual(new object?[] { true, false }, table[1].Values);
    }

    [TestMethod]
    public void IsNullPredicates_ShouldSelectOnlyTheMatchingRows()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "value" }
                ]
            }
        };

        var nullRows = CreateAndRunVirtualMachine(
                "select Name, Name is null as IsNull from #A.Entities() where Name is null",
                sources)
            .Run(TestContext.CancellationToken);
        var nonNullRows = CreateAndRunVirtualMachine(
                "select Name, Name is not null as IsNotNull from #A.Entities() where Name is not null",
                sources)
            .Run(TestContext.CancellationToken);

        Assert.AreEqual(1, nullRows.Count);
        Assert.IsNull(nullRows[0].Values[0]);
        Assert.AreEqual(true, nullRows[0].Values[1]);

        Assert.AreEqual(1, nonNullRows.Count);
        Assert.AreEqual("value", nonNullRows[0].Values[0]);
        Assert.AreEqual(true, nonNullRows[0].Values[1]);
    }

    [TestMethod]
    public void ProjectionOrdinaryComparisonsWithNull_ShouldReturnUnknownForEveryOperator()
    {
        const string query = "select Name = null as EqualResult, Name <> null as NotEqualResult, Name != null as BangNotEqualResult, Name < null as LessResult, Name <= null as LessOrEqualResult, Name > null as GreaterResult, Name >= null as GreaterOrEqualResult from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "value" }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        foreach (var row in table)
        {
            Assert.HasCount(7, row.Values);
            foreach (var value in row.Values)
                Assert.IsNull(value);
        }
    }

    [TestMethod]
    public void LogicalPredicatesAndBetween_ShouldPreserveThreeValuedResults()
    {
        const string query = "select null and true as NullAndTrue, null and false as NullAndFalse, null or true as NullOrTrue, null or false as NullOrFalse, not null as NotNull, 5 between null and 10 as LowerNull, 5 between 1 and null as UpperNull, null between 1 and 10 as ExpressionNull from #A.Entities()";

        var table = CreateAndRunVirtualMachine(
                query,
                new Dictionary<string, IEnumerable<BasicEntity>>
                {
                    ["#A"] = [new BasicEntity()]
                })
            .Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        CollectionAssert.AreEqual(
            new object?[] { null, false, true, null, null, null, null, null },
            table[0].Values);
    }

    [TestMethod]
    public void Contains_ShouldUseLiteralMembershipAndHonorExplicitNullItems()
    {
        const string query = "select Name contains ('ABC', 'CDA', null) as WithNull, Name contains ('ABC', 'CDA') as WithoutNull from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "ABC" },
                    new BasicEntity { Name = "CDA" },
                    new BasicEntity { Name = "other" },
                    new BasicEntity { Name = null }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);
        CollectionAssert.AreEqual(new object?[] { true, true }, table[0].Values);
        CollectionAssert.AreEqual(new object?[] { true, true }, table[1].Values);
        CollectionAssert.AreEqual(new object?[] { false, false }, table[2].Values);
        CollectionAssert.AreEqual(new object?[] { true, false }, table[3].Values);
    }

    [TestMethod]
    public void NullComparisonWarningEnvelope_ShouldPreserveExactPublicContract()
    {
        const string query = "select Name from #A.Entities() where Name = null";
        var result = Analyze(query);
        var warning = result.Warnings.Single(item => item.Code == DiagnosticCode.MQ5017_NullComparison);

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.AreEqual(DiagnosticSeverity.Warning, warning.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, warning.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, warning.SourceKind);

        var expectedStart = query.IndexOf("null", StringComparison.Ordinal);
        Assert.AreEqual(expectedStart, warning.Span.Start);
        Assert.AreEqual(4, warning.Span.Length);
        StringAssert.Contains(warning.Message, "'='");
        Assert.HasCount(0, warning.Arguments);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(warning, query);

        Assert.AreEqual(DiagnosticCode.MQ5017_NullComparison, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Warning, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedStart + 4, envelope.EndOffset);
        Assert.AreEqual(4, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.AreEqual("Core Spec - Null Handling", envelope.DocsReference);
        CollectionAssert.AreEqual(
            new[]
            {
                "Replace the comparison with IS NULL or IS NOT NULL.",
                "Use IS DISTINCT FROM when a total comparison is required."
            },
            envelope.SuggestedFixes.ToArray());
        Assert.HasCount(2, envelope.Actions);
        Assert.IsTrue(envelope.Actions.All(static action =>
            action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));
        Assert.HasCount(0, envelope.Arguments);
    }

    [TestMethod]
    public void DynamicInvalidRegex_ShouldFailAtRuntimeWithInternalExecutionEnvelope()
    {
        const string query = "select Name from #A.Entities() where Name rlike City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [new BasicEntity { Name = "value", City = "[invalid(" }]
            }
        };

        var queryObject = CreateAndRunVirtualMachine(query, sources);
        var exception = Assert.Throws<QueryExecutionException>(() => _ = queryObject.Run(TestContext.CancellationToken).Count);
        var envelope = exception.Envelope;

        Assert.IsNotNull(envelope);
        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Internal, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Internal, envelope.SourceKind);
        Assert.IsNull(envelope.Offset);
        Assert.IsNull(envelope.Length);
        Assert.IsTrue(envelope.Arguments.ContainsKey("correlationId"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.CorrelationId));
        Assert.IsNotNull(exception.InnerException);
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        var provider = new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = []
            });
        return new QueryAnalyzer(provider).Analyze(query);
    }
}
