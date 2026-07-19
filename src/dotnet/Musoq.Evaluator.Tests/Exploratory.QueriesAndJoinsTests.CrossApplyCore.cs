using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryQueriesAndJoinsTests
{
    #region Exploration 21: Complex WHERE with Cross Apply

    [TestMethod]
    public void Explore21_CrossApply_WhereWithOr_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            cross apply p.Tags t
            where t.Value = 'a' or t.Value = 'c'";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b", "c"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("t.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "a"], ["John", "c"]);
    }

    [TestMethod]
    public void Explore21_CrossApply_WhereWithIn_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            cross apply p.Tags t
            where t.Value in ('a', 'c', 'e')";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b", "c", "d", "e"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("t.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "a"], ["John", "c"], ["John", "e"]);
    }

    [TestMethod]
    public void Explore21_CrossApply_WhereWithLike_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            cross apply p.Tags t
            where t.Value like 'test%'";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["test1", "test2", "other"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("t.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "test1"], ["John", "test2"]);
    }

    [TestMethod]
    public void Explore21_CrossApply_WhereWithBetween_ShouldWork()
    {
        const string query = @"
            select p.Name, s.Value
            from #schema.first() p
            cross apply p.Scores s
            where s.Value between 20 and 40";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [10, 20, 30, 40, 50] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("s.Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", 20], ["John", 30], ["John", 40]);
    }

    #endregion

    #region Exploration 24: Cross Apply with Arithmetic

    [TestMethod]
    public void Explore24_CrossApply_ArithmeticOnAppliedValue_ShouldWork()
    {
        const string query = @"
            select p.Name, s.Value * 2 as DoubleScore, s.Value + 100 as BonusScore
            from #schema.first() p
            cross apply p.Scores s";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [10, 20, 30] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Name", typeof(string)), ("DoubleScore", typeof(int)), ("BonusScore", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["John", 20, 110], ["John", 40, 120], ["John", 60, 130]);
    }

    [TestMethod]
    public void Explore24_CrossApply_AggregateAfterArithmetic_ShouldWork()
    {
        const string query = @"
            select p.Name, Sum(s.Value * 2) as TotalDoubleScore
            from #schema.first() p
            cross apply p.Scores s
            group by p.Name";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [10, 20, 30] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("TotalDoubleScore", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", 120]);
    }

    #endregion

    #region Exploration 25: Multiple Sources with Cross Apply

    [TestMethod]
    public void Explore25_CrossApply_FromBothJoinedSources_ShouldWork()
    {
        const string query = @"
            select p.Name, pt.Value, oi.ProductName
            from #schema.first() p
            cross apply p.Tags pt
            inner join #schema.second() o on p.Name = o.CustomerName
            cross apply o.Items oi";

        var persons = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["vip"] }
        }.ToArray();

        var orders = new List<Order>
        {
            new()
            {
                OrderId = 1,
                CustomerName = "John",
                Total = 100,
                Items = [new OrderItem { ProductName = "Widget", Quantity = 1, Price = 100 }]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons, orders);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Name", typeof(string)), ("pt.Value", typeof(string)), ("oi.ProductName", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "vip", "Widget"]);
    }

    #endregion

    #region Exploration 30: Cross Apply with Constants

    [TestMethod]
    public void Explore30_CrossApply_SelectConstantWithApplied_ShouldWork()
    {
        const string query = @"
            select 'Prefix' as Prefix, t.Value, 'Suffix' as Suffix
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["tag1"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Prefix", typeof(string)), ("t.Value", typeof(string)), ("Suffix", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Prefix", "tag1", "Suffix"]);
    }

    #endregion

    #region Exploration 36: Cross Apply with Function

    [TestMethod]
    public void Explore36_CrossApply_WithSplitFunction_ShouldWork()
    {
        const string query = @"
            select p.Name, part.Value
            from #schema.first() p
            cross apply p.Split(p.Name, ' ') part";

        var source = new List<Person>
        {
            new() { Name = "John Doe Smith", Age = 30 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("part.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["John Doe Smith", "John"], ["John Doe Smith", "Doe"], ["John Doe Smith", "Smith"]);
    }

    #endregion
}
