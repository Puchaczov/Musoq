using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryCrossApplyBasicsTests
{
    #region Exploration 12: Union with Cross Apply

    [TestMethod]
    public void Explore12_CrossApply_WithUnion_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t
            where p.Age > 25
            union all (Name, Value)
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t
            where p.Age <= 25";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["older"] },
            new() { Name = "Jane", Age = 20, Tags = ["younger"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "John" && (string)r.Values[1] == "older"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Jane" && (string)r.Values[1] == "younger"));
    }

    #endregion

    #region Exploration 13: Self-referential and Manager patterns

    [TestMethod]
    public void Explore13_CrossApply_OnSameArrayTwice_ShouldWork()
    {
        const string query = @"
            select t1.Value as First, t2.Value as Second
            from #schema.first() p
            cross apply p.Tags t1
            cross apply p.Tags t2
            where t1.Value <> t2.Value";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b", "c"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(6, table.Count);
    }

    #endregion

    #region Exploration 18: Complex join conditions

    [TestMethod]
    public void Explore18_CrossApply_JoinWithComplexCondition_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value, o.OrderId
            from #schema.first() p
            cross apply p.Tags t
            inner join #schema.second() o on p.Name = o.CustomerName and o.Total > 50";

        var persons = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["vip"] }
        }.ToArray();

        var orders = new List<Order>
        {
            new() { OrderId = 1, CustomerName = "John", Total = 100 },
            new() { OrderId = 2, CustomerName = "John", Total = 30 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons, orders);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    #endregion
}
