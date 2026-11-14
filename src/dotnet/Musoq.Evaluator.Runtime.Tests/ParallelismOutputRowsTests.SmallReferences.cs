using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Multi.First;
using Musoq.Evaluator.Tests.Schema.Multi.Second;

namespace Musoq.Evaluator.Tests;

public partial class ParallelismOutputRowsTests
{
    [TestMethod]
    [DataRow(ParallelizationMode.None)]
    [DataRow(ParallelizationMode.Full)]
    public void SmallDistinct_ShouldPreserveExactValuesAndNull(ParallelizationMode mode)
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new() { City = "b" }, new() { City = null }, new() { City = "a" }, new() { City = "b" }],
        };
        using var compiled = CreateVirtualMachineWithOptions(
            "select distinct City from #A.Entities()",
            sources,
            new CompilationOptions(mode)
        );
        using var table = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["a"], ["b"], [null]);
    }

    [TestMethod]
    [DataRow(ParallelizationMode.None, false)]
    [DataRow(ParallelizationMode.Full, false)]
    [DataRow(ParallelizationMode.None, true)]
    [DataRow(ParallelizationMode.Full, true)]
    public void SmallJoins_ShouldPreserveDuplicatePairsAndUnmatchedLeftRows(ParallelizationMode mode, bool leftJoin)
    {
        var join = leftJoin ? "left outer join" : "inner join";
        var sql =
            "select first.FirstItem, second.FirstItem from #schema.first() first "
            + join
            + " #schema.second() second on first.FirstItem = second.FirstItem";
        FirstEntity[] first =
        [
            new() { FirstItem = "b" },
            new() { FirstItem = "missing" },
            new() { FirstItem = "a" },
            new() { FirstItem = "a" },
        ];
        SecondEntity[] second = [new() { FirstItem = "a" }, new() { FirstItem = "b" }, new() { FirstItem = "a" }];
        using var compiled = CreateJoinVirtualMachine(sql, first, second, new CompilationOptions(mode));
        using var table = compiled.Run();
        var expected = SmallJoinExpectedRows(leftJoin);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("FirstItem", typeof(string)),
            ("FirstItem", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, expected);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SmallRepeatedCompilations_ShouldPreserveExactProjectionAndFilter(bool filter)
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new("ten") { Id = 10 }, new("six") { Id = 6 }, new("zero") { Id = 0 }],
        };
        var sql = filter ? "select Name, Id from #A.Entities() where Id % 5 = 0" : "select Name, Id from #A.Entities()";
        object?[][] expected = filter
            ?
            [
                ["ten", 10],
                ["zero", 0],
            ]
            :
            [
                ["ten", 10],
                ["six", 6],
                ["zero", 0],
            ];

        AssertRepeatedSmallResult(sql, sources, expected);
    }

    [TestMethod]
    [DataRow(ParallelizationMode.None)]
    [DataRow(ParallelizationMode.Full)]
    public void SmallStringFilter_ShouldPreserveExactMatchingRows(ParallelizationMode mode)
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new("x500y") { Id = 9 },
                new("500") { Id = 2 },
                new("5000") { Id = 7 },
                new("other") { Id = 500 },
            ],
        };
        using var compiled = CreateVirtualMachineWithOptions(
            "select Name, Id from #A.Entities() where Name like '%500%'",
            sources,
            new CompilationOptions(mode)
        );
        using var table = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["x500y", 9], ["500", 2], ["5000", 7]);
    }

    private static object?[][] SmallJoinExpectedRows(bool leftJoin)
    {
        if (leftJoin)
            return
            [
                ["b", "b"],
                ["a", "a"],
                ["a", "a"],
                ["a", "a"],
                ["a", "a"],
                ["missing", null],
            ];
        return
        [
            ["b", "b"],
            ["a", "a"],
            ["a", "a"],
            ["a", "a"],
            ["a", "a"],
        ];
    }

    private static void AssertRepeatedSmallResult(
        string sql,
        Dictionary<string, IEnumerable<BasicEntity>> sources,
        object?[][] expected
    )
    {
        for (var iteration = 0; iteration < 3; iteration++)
        {
            using var compiled = CreateVirtualMachineWithOptions(
                sql,
                sources,
                new CompilationOptions(ParallelizationMode.Full)
            );
            using var table = compiled.Run();
            TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Id", typeof(int)));
            TableMaterializationTestHelper.AssertRowsUnordered(table, expected);
        }
    }
}
