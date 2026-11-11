using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryQueriesAndJoinsTests
{
    #region Exploration 29: Complex Group By with Cross Apply

    [TestMethod]
    public void Explore29_CrossApply_GroupByMultipleColumns_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select p.Name, s.Value, Count(1) as Cnt\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Scores s\n"
            + "            cross apply p.Tags t\n"
            + "            group by p.Name, s.Value"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Scores = [10, 20],
                Tags = ["a", "b"],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Value", typeof(int)),
            ("Cnt", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", 10, 2L], ["John", 20, 2L]);
    }

    #endregion

    #region Exploration 34: Aggregate with Multiple Conditions

    [TestMethod]
    public void Explore34_ConditionalCount_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Name,\n"
            + "                Count(case when s.Value > 50 then 1 else null end) as HighScoreCount\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Scores s\n"
            + "            group by p.Name"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Scores = [10, 60, 30, 70, 90],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("HighScoreCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", 3L]);
    }

    #endregion

    #region Exploration 35: Nested Aggregates Scenarios

    [TestMethod]
    public void Explore35_MultipleAggregatesInSingleQuery_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Name,\n"
            + "                Count(s.Value) as ScoreCount,\n"
            + "                Sum(s.Value) as TotalScore,\n"
            + "                Avg(s.Value) as AvgScore\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Scores s\n"
            + "            group by p.Name"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Scores = [10, 20, 30],
            },
            new()
            {
                Name = "Jane",
                Age = 25,
                Scores = [5, 15, 25, 35],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("ScoreCount", typeof(long)),
            ("TotalScore", typeof(int?)),
            ("AvgScore", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", 3L, 60, 20m], ["Jane", 4L, 80, 20m]);
    }

    #endregion

    #region Exploration 46: Having clause with cross apply aggregation

    [TestMethod]
    public void Explore46_CrossApply_GroupByHaving_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Name,\n"
            + "                Count(t.Value) as TagCount\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Tags t\n"
            + "            group by p.Name\n"
            + "            having Count(t.Value) > 1"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["a", "b", "c"],
            },
            new()
            {
                Name = "Jane",
                Age = 25,
                Tags = ["x"],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("TagCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", 3L]);
    }

    #endregion

    #region Exploration 47: Distinct with cross apply

    [TestMethod]
    public void Explore47_CrossApply_Distinct_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select distinct t.Value\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Tags t"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["a", "b", "a"],
            },
            new()
            {
                Name = "Jane",
                Age = 25,
                Tags = ["b", "c"],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["a"], ["b"], ["c"]);
    }

    #endregion
}
