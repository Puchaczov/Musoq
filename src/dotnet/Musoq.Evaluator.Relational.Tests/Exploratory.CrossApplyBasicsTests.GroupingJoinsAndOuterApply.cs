using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryCrossApplyBasicsTests
{
    #region Exploration 5: Cross Apply with Group By

    [TestMethod]
    public void Explore5_CrossApply_WithGroupByOnSource_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select p.Name, Count(t.Value) as TagCount\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Tags t\n"
            + "            group by p.Name"
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
                Tags = ["x", "y"],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("TagCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["John", 3L], ["Jane", 2L]);
    }

    [TestMethod]
    public void Explore5_CrossApply_WithGroupByOnAppliedValue_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select t.Value, Count(p.Name) as PersonCount\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Tags t\n"
            + "            group by t.Value"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["common", "unique1"],
            },
            new()
            {
                Name = "Jane",
                Age = 25,
                Tags = ["common", "unique2"],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(string)), ("PersonCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["common", 2L], ["unique1", 1L], ["unique2", 1L]);
    }

    [TestMethod]
    public void Explore5_CrossApply_WithGroupByHaving_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select p.Name, Sum(s.Value) as TotalScore\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Scores s\n"
            + "            group by p.Name\n"
            + "            having Sum(s.Value) > 10"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Scores = [1, 2, 3],
            },
            new()
            {
                Name = "Jane",
                Age = 25,
                Scores = [10, 20, 30],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("TotalScore", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Jane", 60]);
    }

    #endregion

    #region Exploration 6: Cross Apply with Join

    [TestMethod]
    public void Explore6_CrossApply_ThenInnerJoin_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select p.Name, t.Value, o.OrderId\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Tags t\n"
            + "            inner join #schema.second() o on p.Name = o.CustomerName"
        );

        var persons = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["vip"],
            },
        }.ToArray();

        var orders = new List<Order>
        {
            new()
            {
                OrderId = 1,
                CustomerName = "John",
                Total = 100,
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons, orders);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Value", typeof(string)),
            ("OrderId", typeof(int))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "vip", 1]);
    }

    [TestMethod]
    public void Explore6_InnerJoin_ThenCrossApply_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select p.Name, o.OrderId, t.Value\n"
            + "            from #schema.first() p\n"
            + "            inner join #schema.second() o on p.Name = o.CustomerName\n"
            + "            cross apply p.Tags t"
        );

        var persons = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["vip", "premium"],
            },
        }.ToArray();

        var orders = new List<Order>
        {
            new()
            {
                OrderId = 1,
                CustomerName = "John",
                Total = 100,
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons, orders);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("OrderId", typeof(int)),
            ("Value", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", 1, "vip"], ["John", 1, "premium"]);
    }

    [TestMethod]
    public void Explore6_CrossApply_ThenLeftJoin_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select p.Name, t.Value, o.OrderId\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Tags t\n"
            + "            left outer join #schema.second() o on p.Name = o.CustomerName"
        );

        var persons = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["vip"],
            },
            new()
            {
                Name = "Jane",
                Age = 25,
                Tags = ["new"],
            },
        }.ToArray();

        var orders = new List<Order>
        {
            new()
            {
                OrderId = 1,
                CustomerName = "John",
                Total = 100,
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons, orders);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Value", typeof(string)),
            ("OrderId", typeof(int?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "vip", 1], ["Jane", "new", null]);
    }

    #endregion

    #region Exploration 7: Outer Apply Edge Cases

    [TestMethod]
    public void Explore7_OuterApply_WithEmptyArray_ShouldReturnNull()
    {
        const string query = (
            "\n"
            + "            select p.Name, t.Value\n"
            + "            from #schema.first() p\n"
            + "            outer apply p.Tags t"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = [],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", null]);
    }

    [TestMethod]
    public void Explore7_OuterApply_WithNullArray_ShouldReturnNull()
    {
        const string query = (
            "\n"
            + "            select p.Name, t.Value\n"
            + "            from #schema.first() p\n"
            + "            outer apply p.Tags t"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = null,
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", null]);
    }

    [TestMethod]
    public void Explore7_MixedCrossAndOuterApply_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select p.Name, t.Value, s.Value\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Tags t\n"
            + "            outer apply p.Scores s"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["a"],
                Scores = [],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Value", typeof(string)),
            ("Value", typeof(int?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "a", null]);
    }

    #endregion

    #region Exploration 14: Aggregation edge cases

    [TestMethod]
    public void Explore14_CrossApply_WithMultipleAggregates_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Name,\n"
            + "                Count(s.Value) as ScoreCount,\n"
            + "                Sum(s.Value) as TotalScore,\n"
            + "                Avg(s.Value) as AvgScore,\n"
            + "                Min(s.Value) as MinScore,\n"
            + "                Max(s.Value) as MaxScore\n"
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
                Scores = [10, 20, 30, 40, 50],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("ScoreCount", typeof(long)),
            ("TotalScore", typeof(int?)),
            ("AvgScore", typeof(decimal?)),
            ("MinScore", typeof(int?)),
            ("MaxScore", typeof(int?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", 5L, 150, 30m, 10, 50]);
    }

    [TestMethod]
    public void Explore14_CrossApply_AggregateWithoutGroupBy_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                Count(s.Value) as TotalScores\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Scores s"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Scores = [1, 2, 3],
            },
            new()
            {
                Name = "Jane",
                Age = 25,
                Scores = [4, 5],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("TotalScores", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [5L]);
    }

    #endregion
}
