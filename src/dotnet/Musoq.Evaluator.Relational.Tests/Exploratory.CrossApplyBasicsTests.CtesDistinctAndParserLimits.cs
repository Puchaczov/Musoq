using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryCrossApplyBasicsTests
{
    #region Exploration 8: CTE with Cross Apply

    [TestMethod]
    public void Explore8_Cte_ThenCrossApply_ShouldWork()
    {
        const string query = (
            "\n"
            + "            with cte as (\n"
            + "                select p.Name as Name, p.Tags as Tags\n"
            + "                from #schema.first() p\n"
            + "            )\n"
            + "            select c.Name, t.Value\n"
            + "            from cte c\n"
            + "            cross apply c.Tags t"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["a", "b"],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "a"], ["John", "b"]);
    }

    [TestMethod]
    public void Explore8_Cte_WithMultipleCrossApplies_ShouldWork()
    {
        const string query = (
            "\n"
            + "            with cte as (\n"
            + "                select p.Name as Name, p.Tags as Tags, p.Scores as Scores\n"
            + "                from #schema.first() p\n"
            + "            )\n"
            + "            select c.Name, t.Value, s.Value\n"
            + "            from cte c\n"
            + "            cross apply c.Tags t\n"
            + "            cross apply c.Scores s"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["a"],
                Scores = [1, 2],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Value", typeof(string)),
            ("Value", typeof(int))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "a", 1], ["John", "a", 2]);
    }

    [TestMethod]
    public void Explore8_MultipleCtes_WithCrossApply_ShouldWork()
    {
        const string query = (
            "\n"
            + "            with cte1 as (\n"
            + "                select p.Name as Name, p.Tags as Tags\n"
            + "                from #schema.first() p\n"
            + "            ),\n"
            + "            cte2 as (\n"
            + "                select c.Name as PersonName, t.Value as TagValue\n"
            + "                from cte1 c\n"
            + "                cross apply c.Tags t\n"
            + "            )\n"
            + "            select PersonName, TagValue\n"
            + "            from cte2"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["a", "b"],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("PersonName", typeof(string)),
            ("TagValue", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "a"], ["John", "b"]);
    }

    #endregion

    #region Exploration 9: Subqueries (Parser limitations - subqueries not fully supported)

    [TestMethod]
    public void Explore9_CrossApply_InSubquery_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select *\n"
            + "            from (\n"
            + "                select p.Name, t.Value\n"
            + "                from #schema.first() p\n"
            + "                cross apply p.Tags t\n"
            + "            ) sub"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["a", "b"],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "a"], ["John", "b"]);
    }

    [TestMethod]
    public void Explore9_SubqueryWithCrossApply_JoinedWithTable_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select sub.Name, sub.Tag, o.OrderId\n"
            + "            from (\n"
            + "                select p.Name as Name, t.Value as Tag\n"
            + "                from #schema.first() p\n"
            + "                cross apply p.Tags t\n"
            + "            ) sub\n"
            + "            inner join #schema.second() o on sub.Name = o.CustomerName"
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
            ("Tag", typeof(string)),
            ("OrderId", typeof(int))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "vip", 1]);
    }

    #endregion

    #region Exploration 11: Distinct with Cross Apply

    [TestMethod]
    public void Explore11_CrossApply_WithDistinct_ShouldWork()
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
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["a"], ["b"], ["c"]);
    }

    [TestMethod]
    public void Explore11_CrossApply_DistinctOnMultipleColumns_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select distinct p.Name, t.Value\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Tags t\n"
            + "            cross apply p.Tags t2"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["a", "b"],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["John", "a"], ["John", "b"]);
    }

    #endregion
}
