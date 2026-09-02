using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticCross065JoinApplyAggregateWindowTests : BasicEntityTestBase
{
    [TestMethod]
    public void JoinApplyAggregateWindowAndOrdering_ShouldPreserveAliasesAndNulls()
    {
        const string query = @"
            select a.City as City,
                   child.Name as ChildName,
                   Count(b.Name) as Matches,
                   RowNumber() over (
                       partition by a.City
                       order by child.Name asc
                   ) as rn
            from #A.entities() a
            left outer join #B.entities() b on a.City = b.City
            cross apply a.Children child
            group by a.City, child.Name
            having Count(b.Name) >= 0
            qualify RowNumber() over (
                partition by a.City
                order by child.Name asc
            ) = 1
            order by City desc nulls last, ChildName asc
            take 3";

        var table = TableMaterializationTestHelper.Materialize(
            CreateAndRunVirtualMachine(
                query,
                new Dictionary<string, IEnumerable<BasicEntity>>
                {
                    ["#A"] =
                    [
                        new BasicEntity("parent-1") { City = "Paris", Id = 1 },
                        new BasicEntity("parent-2") { City = "Berlin", Id = 2 },
                        new BasicEntity("parent-3") { City = null, Id = 3 }
                    ],
                    ["#B"] =
                    [
                        new BasicEntity("match") { City = "Paris", Id = 10 }
                    ]
                }).Run(TestContext.CancellationToken));

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("ChildName", typeof(string)),
            ("Matches", typeof(long)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Paris", "child1", 1L, 1L],
            ["Berlin", "child1", 0L, 1L],
            [null, "child1", 0L, 1L]);
    }

    [TestMethod]
    public void MalformedCrossFeatureVariants_ShouldReportFocusedDiagnosticsWithoutCascades()
    {
        var provider = new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [],
                ["#B"] = []
            });

        const string windowInHaving = @"
            select a.City, Count(*) as Rows
            from #A.entities() a
            inner join #B.entities() b on a.Id = b.Id
            group by a.City
            having RowNumber() over (order by a.City) = 1";
        var windowResult = new QueryAnalyzer(provider).Analyze(windowInHaving);
        AssertSingleDiagnostic(
            windowResult,
            DiagnosticCode.MQ3101_WindowFunctionInFilter,
            "Window functions are not allowed in HAVING");

        const string aggregateInGroupBy = @"
            select a.City, Count(*) as Rows
            from #A.entities() a
            cross apply a.Children child
            group by a.City, Count(child.Name)";
        var aggregateResult = new QueryAnalyzer(provider).Analyze(aggregateInGroupBy);
        AssertSingleDiagnostic(
            aggregateResult,
            DiagnosticCode.MQ3092_AggregateInGroupBy,
            "GROUP BY expressions cannot contain aggregate functions");
    }

    private static void AssertSingleDiagnostic(
        QueryAnalysisResult result,
        DiagnosticCode expectedCode,
        string expectedMessage)
    {
        var diagnostics = result.Errors.ToArray();
        Assert.HasCount(1, diagnostics, string.Join(" | ", result.Diagnostics.Select(static item => item.Message)));
        Assert.AreEqual(expectedCode, diagnostics[0].Code);
        StringAssert.Contains(diagnostics[0].Message, expectedMessage);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostics[0].Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostics[0].SourceKind);
        Assert.IsTrue(diagnostics[0].Location.IsValid);
        Assert.IsTrue(diagnostics[0].EndLocation.IsValid);
    }
}
