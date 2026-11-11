using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryCrossApplyBasicsTests
{
    #region Exploration 12: Union with Cross Apply

    [TestMethod]
    public void Explore12_CrossApply_WithUnion_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Name,\n"
            + "                t.Value\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Tags t\n"
            + "            where p.Age > 25\n"
            + "            union all (Name, Value)\n"
            + "            select\n"
            + "                p.Name,\n"
            + "                t.Value\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Tags t\n"
            + "            where p.Age <= 25"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["older"],
            },
            new()
            {
                Name = "Jane",
                Age = 20,
                Tags = ["younger"],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["John", "older"], ["Jane", "younger"]);
    }

    #endregion

    #region Exploration 13: Self-referential and Manager patterns

    [TestMethod]
    public void Explore13_CrossApply_OnSameArrayTwice_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select t1.Value as First, t2.Value as Second\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Tags t1\n"
            + "            cross apply p.Tags t2\n"
            + "            where t1.Value <> t2.Value"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["a", "b", "c"],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("First", typeof(string)), ("Second", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["a", "b"],
            ["a", "c"],
            ["b", "a"],
            ["b", "c"],
            ["c", "a"],
            ["c", "b"]
        );
    }

    #endregion

    #region Exploration 18: Complex join conditions

    [TestMethod]
    public void Explore18_CrossApply_JoinWithComplexCondition_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select p.Name, t.Value, o.OrderId\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Tags t\n"
            + "            inner join #schema.second() o on p.Name = o.CustomerName and o.Total > 50"
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
            new()
            {
                OrderId = 2,
                CustomerName = "John",
                Total = 30,
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

    #endregion
}
