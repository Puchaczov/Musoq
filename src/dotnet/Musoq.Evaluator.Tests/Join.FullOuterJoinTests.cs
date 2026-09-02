using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.IR;
using Musoq.Evaluator.Tests.Schema.Basic;
using PhysicalPlanPrinter = Musoq.Evaluator.IR.Physical.PhysicalPlanPrinter;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class JoinFullOuterJoinTests : BasicEntityTestBase
{

    [TestMethod]
    public void FullOuterJoin_WithOnlyMatches_ShouldReturnMatchedRows()
    {
        const string query = "select a.Id, b.Id from #A.entities() a full outer join #B.entities() b on a.Id = b.Id";
        var sources = CreateSources(
            [new BasicEntity("a") { Id = 1 }],
            [new BasicEntity("b") { Id = 1 }]);

        var table = Run(query, sources);

        AssertFullOuterIdColumns(table);
        AssertRows(table, (1, 1));
    }

    [TestMethod]
    public void FullOuterJoin_WithLeftOnlyAndRightOnlyRows_ShouldReturnNullExtendedRows()
    {
        const string query = "select a.Id, b.Id from #A.entities() a full outer join #B.entities() b on a.Id = b.Id";
        var sources = CreateSources(
            [
                new BasicEntity("a1") { Id = 1 },
                new BasicEntity("a2") { Id = 2 }
            ],
            [
                new BasicEntity("b2") { Id = 2 },
                new BasicEntity("b3") { Id = 3 }
            ]);

        var table = Run(query, sources);

        AssertFullOuterIdColumns(table);
        AssertRows(table, (1, null), (2, 2), (null, 3));
    }

    [TestMethod]
    public void FullOuterJoin_WithEmptyInputs_ShouldReturnNoRows()
    {
        const string query = "select a.Id, b.Id from #A.entities() a full outer join #B.entities() b on a.Id = b.Id";
        var table = Run(query, CreateSources([], []));

        AssertFullOuterIdColumns(table);
        AssertRows(table);
    }

    [TestMethod]
    public void FullOuterJoin_WithEmptyLeft_ShouldReturnRightOnlyRows()
    {
        const string query = "select a.Id, b.Id from #A.entities() a full outer join #B.entities() b on a.Id = b.Id";
        var table = Run(query, CreateSources([], [new BasicEntity("b") { Id = 7 }]));

        AssertFullOuterIdColumns(table);
        AssertRows(table, (null, 7));
    }

    [TestMethod]
    public void FullOuterJoin_WithEmptyRight_ShouldReturnLeftOnlyRows()
    {
        const string query = "select a.Id, b.Id from #A.entities() a full outer join #B.entities() b on a.Id = b.Id";
        var table = Run(query, CreateSources([new BasicEntity("a") { Id = 5 }], []));

        AssertFullOuterIdColumns(table);
        AssertRows(table, (5, null));
    }

    [TestMethod]
    public void FullOuterJoin_WithDuplicateKeys_ShouldPreserveJoinMultiplicity()
    {
        const string query = @"
select a.Name, b.Name
from #A.entities() a
full outer join #B.entities() b on a.Id = b.Id";
        var sources = CreateSources(
            [
                new BasicEntity { Id = 1, Name = "a1" },
                new BasicEntity { Id = 1, Name = "a2" }
            ],
            [
                new BasicEntity { Id = 1, Name = "b1" },
                new BasicEntity { Id = 1, Name = "b2" }
            ]);

        var table = Run(query, sources);

        AssertFullOuterNameColumns(table);
        AssertStringRows(table, ("a1", "b1"), ("a1", "b2"), ("a2", "b1"), ("a2", "b2"));
    }

    [TestMethod]
    public void FullOuterJoin_WithNonEquiPredicate_ShouldUseNestedLoopSemantics()
    {
        const string query = "select a.Id, b.Id from #A.entities() a full outer join #B.entities() b on a.Id < b.Id";
        var sources = CreateSources(
            [
                new BasicEntity("a1") { Id = 1 },
                new BasicEntity("a3") { Id = 3 }
            ],
            [
                new BasicEntity("b2") { Id = 2 },
                new BasicEntity("b4") { Id = 4 }
            ]);

        var table = Run(query, sources);

        AssertFullOuterIdColumns(table);
        AssertRows(table, (1, 2), (1, 4), (3, 4));
    }

    [TestMethod]
    public void FullOuterJoin_BetweenCtes_ShouldReturnRows()
    {
        const string query = @"
with cteA as (
    select Id, Name from #A.entities()
),
cteB as (
    select Id, Name from #B.entities()
)
select a.Name, b.Name
from cteA a
full outer join cteB b on a.Id = b.Id";
        var sources = CreateSources(
            [
                new BasicEntity { Id = 1, Name = "left" },
                new BasicEntity { Id = 2, Name = "match-left" }
            ],
            [
                new BasicEntity { Id = 2, Name = "match-right" },
                new BasicEntity { Id = 3, Name = "right" }
            ]);

        var table = Run(query, sources);

        AssertFullOuterNameColumns(table);
        AssertStringRows(table, ("left", null), ("match-left", "match-right"), (null, "right"));
    }

    [TestMethod]
    public void FullOuterJoin_ShouldLiftValueTypesOnBothSides()
    {
        const string query = "select a.Id, b.Id from #A.entities() a full outer join #B.entities() b on a.Id = b.Id";
        var table = Run(query, CreateSources([new BasicEntity("a") { Id = 1 }], [new BasicEntity("b") { Id = 2 }]));

        AssertFullOuterIdColumns(table);
    }

    [TestMethod]
    public void FullOuterJoin_WithEquiPredicate_ShouldUseHashJoin()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Id, b.Id from #A.entities() a full outer join #B.entities() b on a.Id = b.Id");

        var physicalText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        Assert.Contains("PhysicalHashJoin [FullOuter] [build: b.Id] [probe: a.Id]", physicalText);
    }

    [TestMethod]
    public void FullOuterJoin_WithHashDisabled_ShouldUseNestedLoopAndReturnSameRows()
    {
        const string query = "select a.Id, b.Id from #A.entities() a full outer join #B.entities() b on a.Id = b.Id";
        var sources = CreateSources(
            [
                new BasicEntity("a1") { Id = 1 },
                new BasicEntity("a2") { Id = 2 }
            ],
            [
                new BasicEntity("b2") { Id = 2 },
                new BasicEntity("b3") { Id = 3 }
            ]);
        var inspection = Inspect(
            query,
            new CompilationOptions(useHashJoin: false, useSortMergeJoin: false));

        var table = Run(query, sources, new CompilationOptions(useHashJoin: false, useSortMergeJoin: false));

        Assert.Contains("PhysicalNestedLoopJoin [FullOuter]", inspection.PhysicalPlanText);
        AssertFullOuterIdColumns(table);
        AssertRows(table, (1, null), (2, 2), (null, 3));
    }

    [TestMethod]
    public void FullOuterJoin_WithCompositeKeys_ShouldReturnHashJoinRows()
    {
        const string query = @"
select a.Id, b.Id
from #A.entities() a
full outer join #B.entities() b on a.Id = b.Id and a.City = b.City";
        var sources = CreateSources(
            [
                new BasicEntity { Id = 1, City = "x" },
                new BasicEntity { Id = 1, City = "left" }
            ],
            [
                new BasicEntity { Id = 1, City = "x" },
                new BasicEntity { Id = 1, City = "right" }
            ]);

        var table = Run(query, sources);

        AssertFullOuterIdColumns(table);
        AssertRows(table, (1, 1), (1, null), (null, 1));
    }

    [TestMethod]
    public void FullOuterJoin_WithResidualPredicate_ShouldOnlyMarkResidualMatches()
    {
        const string query = @"
select a.Id, b.Id
from #A.entities() a
full outer join #B.entities() b on a.Id = b.Id and b.City = 'match'";
        var sources = CreateSources(
            [
                new BasicEntity { Id = 1 },
                new BasicEntity { Id = 2 }
            ],
            [
                new BasicEntity { Id = 1, City = "reject" },
                new BasicEntity { Id = 2, City = "match" }
            ]);

        var table = Run(query, sources);

        AssertFullOuterIdColumns(table);
        AssertRows(table, (1, null), (null, 1), (2, 2));
    }

    [TestMethod]
    public void FullOuterJoin_WithNonEquiPredicate_ShouldReportNestedLoopFallback()
    {
        var inspection = Inspect(
            "select a.Id, b.Id from #A.entities() a full outer join #B.entities() b on a.Id < b.Id",
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        Assert.Contains("PhysicalNestedLoopJoin [FullOuter]", inspection.PhysicalPlanText);
        Assert.Contains("No hash-join equi key pair was found and sort-merge join is disabled.", inspection.PlanningText);
    }

    private Table Run(string query, IDictionary<string, IEnumerable<BasicEntity>> sources)
    {
        return Run(query, sources, new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));
    }

    private Table Run(
        string query,
        IDictionary<string, IEnumerable<BasicEntity>> sources,
        CompilationOptions options)
    {
        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            options);

        return TableMaterializationTestHelper.Materialize(vm.Run(TestContext.CancellationToken));
    }

    private QueryInspectionResult Inspect(string query, CompilationOptions options)
    {
        return InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(CreateSources([], [])),
            LoggerResolver,
            options);
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

    private static void AssertRows(Table table, params (int? Left, int? Right)[] expected)
    {
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            expected.Select(static row => new object?[] { row.Left, row.Right }).ToArray());
    }

    private static void AssertStringRows(Table table, params (string? Left, string? Right)[] expected)
    {
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            expected.Select(static row => new object?[] { row.Left, row.Right }).ToArray());
    }

    private static void AssertFullOuterIdColumns(Table table)
    {
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int?)),
            ("b.Id", typeof(int?)));
    }

    private static void AssertFullOuterNameColumns(Table table)
    {
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)));
    }
}
