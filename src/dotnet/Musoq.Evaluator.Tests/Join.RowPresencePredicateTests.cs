using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class JoinRowPresencePredicateTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void LeftJoin_CaseWhenRightAliasIsMissing_ShouldClassifyRows()
    {
        const string query = @"
select
    case when b is missing then 'LeftOnly' else 'Matched' end as State,
    a.Id,
    b.Id
from #A.entities() a
left join #B.entities() b on a.Id = b.Id";

        var table = Run(query, CreateSources(
            [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }],
            [new BasicEntity { Id = 2 }]));

        Assert.AreEqual(2, table.Count);
        AssertRows(table, ("LeftOnly", 1, null), ("Matched", 2, 2));
    }

    [TestMethod]
    public void RightJoin_CaseWhenLeftAliasIsMissing_ShouldClassifyRows()
    {
        const string query = @"
select
    case when a is missing then 'RightOnly' else 'Matched' end as State,
    a.Id,
    b.Id
from #A.entities() a
right join #B.entities() b on a.Id = b.Id";

        var table = Run(query, CreateSources(
            [new BasicEntity { Id = 2 }],
            [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }]));

        Assert.AreEqual(2, table.Count);
        AssertRows(table, ("RightOnly", null, 1), ("Matched", 2, 2));
    }

    [TestMethod]
    public void FullOuterJoin_CaseWhenAliasesAreMissing_ShouldClassifyAllRowKinds()
    {
        const string query = @"
select
    case
        when b is missing then 'LeftOnly'
        when a is missing then 'RightOnly'
        else 'Matched'
    end as State,
    a.Id,
    b.Id
from #A.entities() a
full outer join #B.entities() b on a.Id = b.Id";

        var table = Run(query, CreateSources(
            [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }],
            [new BasicEntity { Id = 2 }, new BasicEntity { Id = 3 }]));

        Assert.AreEqual(3, table.Count);
        AssertRows(table, ("LeftOnly", 1, null), ("Matched", 2, 2), ("RightOnly", null, 3));
    }

    [TestMethod]
    public void LeftJoin_RowPresence_ShouldNotDependOnNullableColumnValues()
    {
        const string query = @"
select
    a.Id,
    case
        when b is present then 'Present'
        when b is missing then 'Missing'
        else 'Impossible'
    end as State,
    b.NullableValue
from #A.entities() a
left join #B.entities() b on a.Id = b.Id";

        var table = Run(query, CreateSources(
            [
                new BasicEntity { Id = 1 },
                new BasicEntity { Id = 2 }
            ],
            [
                new BasicEntity { Id = 1, NullableValue = null }
            ]));

        Assert.AreEqual(2, table.Count);
        AssertRows(table, (1, "Present", null), (2, "Missing", null));
    }

    [TestMethod]
    public void FullOuterJoin_WhereAliasIsMissing_ShouldReturnOnlyRowsWithoutThatSide()
    {
        const string query = @"
select a.Id, b.Id
from #A.entities() a
full outer join #B.entities() b on a.Id = b.Id
where b is missing";

        var table = Run(query, CreateSources(
            [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }],
            [new BasicEntity { Id = 2 }, new BasicEntity { Id = 3 }]));

        Assert.AreEqual(1, table.Count);
        AssertRows(table, (1, null));
    }

    [TestMethod]
    public void LeftJoin_WhereRightAliasPresencePredicates_ShouldFilterRows()
    {
        const string missingQuery = @"
select a.Id, b.Id
from #A.entities() a
left join #B.entities() b on a.Id = b.Id
where b is missing";

        const string presentQuery = @"
select a.Id, b.Id
from #A.entities() a
left join #B.entities() b on a.Id = b.Id
where b is present";

        var sources = CreateSources(
            [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }],
            [new BasicEntity { Id = 2 }]);

        var missing = Run(missingQuery, sources);
        var present = Run(presentQuery, sources);

        Assert.AreEqual(1, missing.Count);
        AssertRows(missing, (1, null));
        Assert.AreEqual(1, present.Count);
        AssertRows(present, (2, 2));
    }

    [TestMethod]
    public void RightJoin_WhereLeftAliasPresencePredicates_ShouldFilterRows()
    {
        const string missingQuery = @"
select a.Id, b.Id
from #A.entities() a
right join #B.entities() b on a.Id = b.Id
where a is missing";

        const string presentQuery = @"
select a.Id, b.Id
from #A.entities() a
right join #B.entities() b on a.Id = b.Id
where a is present";

        var sources = CreateSources(
            [new BasicEntity { Id = 2 }],
            [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }]);

        var missing = Run(missingQuery, sources);
        var present = Run(presentQuery, sources);

        Assert.AreEqual(1, missing.Count);
        AssertRows(missing, (null, 1));
        Assert.AreEqual(1, present.Count);
        AssertRows(present, (2, 2));
    }

    [TestMethod]
    public void FullOuterJoin_WhereAliasPresencePredicates_ShouldFilterRows()
    {
        const string leftMissingQuery = @"
select a.Id, b.Id
from #A.entities() a
full outer join #B.entities() b on a.Id = b.Id
where a is missing";

        const string bothPresentQuery = @"
select a.Id, b.Id
from #A.entities() a
full outer join #B.entities() b on a.Id = b.Id
where a is present and b is present";

        var sources = CreateSources(
            [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }],
            [new BasicEntity { Id = 2 }, new BasicEntity { Id = 3 }]);

        var leftMissing = Run(leftMissingQuery, sources);
        var bothPresent = Run(bothPresentQuery, sources);

        Assert.AreEqual(1, leftMissing.Count);
        AssertRows(leftMissing, (null, 3));
        Assert.AreEqual(1, bothPresent.Count);
        AssertRows(bothPresent, (2, 2));
    }

    [TestMethod]
    public void AsOfLeftJoin_WhereRightAliasPresencePredicates_ShouldFilterRows()
    {
        const string missingQuery = @"
select a.Name, b.Name
from #A.entities() a
asof left join #B.entities() b on a.Population >= b.Population
where b is missing";

        const string presentQuery = @"
select a.Name, b.Name
from #A.entities() a
asof left join #B.entities() b on a.Population >= b.Population
where b is present";

        var sources = CreateSources(
            [
                new BasicEntity { Name = "A1", Population = 100 },
                new BasicEntity { Name = "A2", Population = 1 }
            ],
            [
                new BasicEntity { Name = "B1", Population = 50 }
            ]);

        var missing = Run(missingQuery, sources);
        var present = Run(presentQuery, sources);

        Assert.AreEqual(1, missing.Count);
        AssertRows(missing, ("A2", null));
        Assert.AreEqual(1, present.Count);
        AssertRows(present, ("A1", "B1"));
    }

    [TestMethod]
    public void RowPresence_WithSingleSourceAlias_ShouldReportAlwaysPresentAlias()
    {
        const string query = "select a.Id from #A.entities() a where a is missing";

        AssertAlwaysPresentAliasRejected(query, "a");
    }

    [TestMethod]
    public void RowPresence_WithInnerJoinAlias_ShouldReportAlwaysPresentAlias()
    {
        const string query = @"
select a.Id
from #A.entities() a
inner join #B.entities() b on a.Id = b.Id
where b is missing";

        AssertAlwaysPresentAliasRejected(query, "b");
    }

    [TestMethod]
    public void RowPresence_WithCrossJoinAlias_ShouldReportAlwaysPresentAlias()
    {
        const string query = @"
select a.Id
from #A.entities() a
cross join #B.entities() b
where b is missing";

        AssertAlwaysPresentAliasRejected(query, "b");
    }

    [TestMethod]
    public void RowPresence_WithAsOfJoinAlias_ShouldReportAlwaysPresentAlias()
    {
        const string query = @"
select a.Id
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population
where b is missing";

        AssertAlwaysPresentAliasRejected(query, "b");
    }

    [TestMethod]
    public void RowPresence_WithPreservedOuterJoinAlias_ShouldReportAlwaysPresentAlias()
    {
        const string leftJoinQuery = @"
select a.Id
from #A.entities() a
left join #B.entities() b on a.Id = b.Id
where a is missing";

        const string rightJoinQuery = @"
select b.Id
from #A.entities() a
right join #B.entities() b on a.Id = b.Id
where b is missing";

        const string asOfLeftJoinQuery = @"
select a.Id
from #A.entities() a
asof left join #B.entities() b on a.Population >= b.Population
where a is missing";

        AssertAlwaysPresentAliasRejected(leftJoinQuery, "a");
        AssertAlwaysPresentAliasRejected(rightJoinQuery, "b");
        AssertAlwaysPresentAliasRejected(asOfLeftJoinQuery, "a");
    }

    [TestMethod]
    public void RowPresence_InSameJoinOnClause_ShouldReportAlwaysPresentAlias()
    {
        const string query = @"
select a.Id
from #A.entities() a
left join #B.entities() b on b is missing";

        AssertAlwaysPresentAliasRejected(query, "b");
    }

    [TestMethod]
    public void OuterApply_WhereRightAliasPresencePredicates_ShouldFilterRows()
    {
        const string missingQuery = @"
select a.Id, b.Id
from #A.entities() a
outer apply #B.entities() b
where b is missing";

        const string presentQuery = @"
select a.Id, b.Id
from #A.entities() a
outer apply #B.entities() b
where b is present";

        var missing = Run(missingQuery, CreateSources(
            [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }],
            []));
        var present = Run(presentQuery, CreateSources(
            [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }],
            [new BasicEntity { Id = 10 }]));

        Assert.AreEqual(2, missing.Count);
        AssertRows(missing, (1, null), (2, null));
        Assert.AreEqual(2, present.Count);
        AssertRows(present, (1, 10), (2, 10));
    }

    [TestMethod]
    public void RowPresence_WithCrossApplyAlias_ShouldReportAlwaysPresentAlias()
    {
        const string query = @"
select a.Id
from #A.entities() a
cross apply #B.entities() b
where b is missing";

        AssertAlwaysPresentAliasRejected(query, "b");
    }

    [TestMethod]
    public void RowPresence_WithPreservedOuterApplyAlias_ShouldReportAlwaysPresentAlias()
    {
        const string query = @"
select a.Id
from #A.entities() a
outer apply #B.entities() b
where a is missing";

        AssertAlwaysPresentAliasRejected(query, "a");
    }

    [TestMethod]
    public void RowPresence_FromEarlierLeftJoin_ShouldRemainValidAfterLaterInnerJoin()
    {
        const string query = @"
select a.Id, b.Id, c.Id
from #A.entities() a
left join #B.entities() b on a.Id = b.Id
inner join #C.entities() c on a.Id = c.Id
where b is missing";

        var table = Run(query, CreateSources(
            [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }],
            [new BasicEntity { Id = 2 }],
            [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }]));

        Assert.AreEqual(1, table.Count);
        AssertThreeWayRows(table, (1, null, 1));
    }

    [TestMethod]
    public void RowPresence_FromEarlierOuterApply_ShouldRemainValidAfterLaterInnerJoin()
    {
        const string query = @"
select a.Id, b.Id, c.Id
from #A.entities() a
outer apply #B.entities() b
inner join #C.entities() c on a.Id = c.Id
where b is missing";

        var table = Run(query, CreateSources(
            [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }],
            [],
            [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }]));

        Assert.AreEqual(2, table.Count);
        AssertThreeWayRows(table, (1, null, 1), (2, null, 2));
    }

    [TestMethod]
    public void RowPresence_InsideCteWithLeftJoin_ShouldRemainValid()
    {
        const string query = @"
with leftOnly as (
    select a.Id as Id
    from #A.entities() a
    left join #B.entities() b on a.Id = b.Id
    where b is missing
)
select Id from leftOnly";

        var table = Run(query, CreateSources(
            [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }],
            [new BasicEntity { Id = 2 }]));

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
    }

    [TestMethod]
    public void RowPresence_WithUnknownAlias_ShouldReportReadableUnknownAlias()
    {
        const string query = @"
select bb is missing
from #A.entities() a
left join #B.entities() b on a.Id = b.Id";

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources([], [])));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind, "bb");
        AssertMessageContains(ex, "Did you mean 'b'");
    }

    [TestMethod]
    public void RowPresence_WithScalarExpression_ShouldReportReadableMisuse()
    {
        const string query = "select a.Id is missing from #A.entities() a";

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources([], [])));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3007_InvalidOperandTypes, DiagnosticPhase.Bind, "source alias");
        AssertMessageContains(ex, "outer join");
    }

    private Table Run(string query, IDictionary<string, IEnumerable<BasicEntity>> sources)
    {
        var vm = CreateAndRunVirtualMachine(query, sources);
        return TableMaterializationTestHelper.Materialize(vm.Run(TestContext.CancellationToken));
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateSources(
        IEnumerable<BasicEntity> left,
        IEnumerable<BasicEntity> right)
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", left },
            { "#B", right }
        };
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateSources(
        IEnumerable<BasicEntity> left,
        IEnumerable<BasicEntity> right,
        IEnumerable<BasicEntity> third)
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", left },
            { "#B", right },
            { "#C", third }
        };
    }

    private static void AssertRows(Table table, params (string State, int? LeftId, int? RightId)[] expected)
    {
        var rows = table.Select(row => ((string)row[0], (int?)row[1], (int?)row[2])).ToArray();

        foreach (var expectedRow in expected)
            Assert.Contains(expectedRow, rows);
    }

    private static void AssertRows(Table table, params (int? LeftId, int? RightId)[] expected)
    {
        var rows = table.Select(row => ((int?)row[0], (int?)row[1])).ToArray();

        foreach (var expectedRow in expected)
            Assert.Contains(expectedRow, rows);
    }

    private static void AssertRows(Table table, params (int Id, string State, int? NullableValue)[] expected)
    {
        var rows = table.Select(row => ((int)row[0], (string)row[1], (int?)row[2])).ToArray();

        foreach (var expectedRow in expected)
            Assert.Contains(expectedRow, rows);
    }

    private static void AssertThreeWayRows(Table table, params (int LeftId, int? RightId, int ThirdId)[] expected)
    {
        var rows = table.Select(row => ((int)row[0], (int?)row[1], (int)row[2])).ToArray();

        foreach (var expectedRow in expected)
            Assert.Contains(expectedRow, rows);
    }

    private static void AssertRows(Table table, params (string? LeftName, string? RightName)[] expected)
    {
        var rows = table.Select(row => ((string?)row[0], (string?)row[1])).ToArray();

        foreach (var expectedRow in expected)
            Assert.Contains(expectedRow, rows);
    }

    private void AssertAlwaysPresentAliasRejected(string query, string alias)
    {
        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateSources([], [])));
        var expectedMessage =
            $"Row presence predicates require an alias that can be absent because of LEFT, RIGHT, FULL, ASOF LEFT JOIN, or OUTER APPLY. Alias '{alias}' is always present in this scope.";

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3007_InvalidOperandTypes, DiagnosticPhase.Bind, alias);
        Assert.AreEqual(expectedMessage, ex.PrimaryEnvelope.Message);
    }
}
