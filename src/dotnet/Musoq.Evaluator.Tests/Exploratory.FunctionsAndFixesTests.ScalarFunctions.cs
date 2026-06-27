using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryFunctionsAndFixesTests
{
    #region Exploration 99: StartsWith and EndsWith

    [TestMethod]
    public void Explore99_StartsWithEndsWith_ShouldWork()
    {
        const string query = @"
            select
                t.Value,
                StartsWith(t.Value, 'a') as StartsWithA,
                EndsWith(t.Value, 'le') as EndsWithLE
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["apple", "banana", "able"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
    }

    #endregion

    #region Exploration 100: Concat function

    [TestMethod]
    public void Explore100_ConcatFunction_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                Concat(p.Name, '-', ToString(p.Age)) as Combined
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Exploration 101: IsNull function

    [TestMethod]
    public void Explore101_IsNullCheck_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                p.Tags is null as HasNoTags
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = null },
            new() { Name = "Jane", Age = 25, Tags = ["a"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 102: IsNotNull check

    [TestMethod]
    public void Explore102_IsNotNullCheck_ShouldWork()
    {
        const string query = @"
            select
                p.Name
            from #schema.first() p
            where p.Tags is not null";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = null },
            new() { Name = "Jane", Age = 25, Tags = ["a"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Jane", table[0].Values[0]);
    }

    #endregion

    #region Exploration 104: Abs function

    [TestMethod]
    public void Explore104_AbsFunction_ShouldWork()
    {
        const string query = @"
            select
                p.Age,
                Abs(p.Age - 35) as DistanceFrom35
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Jane", Age = 40 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 105: Round function

    [TestMethod]
    public void Explore105_RoundFunction_ShouldWork()
    {
        const string query = @"
            select
                i.ProductName,
                i.Price,
                Round(i.Price, 0) as RoundedPrice
            from #schema.first() o
            cross apply o.Items i";

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items =
                [
                    new OrderItem { ProductName = "A", Price = 10.49m, Quantity = 1 },
                    new OrderItem { ProductName = "B", Price = 10.51m, Quantity = 1 }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 106: Floor function

    [TestMethod]
    public void Explore106_Floor_ShouldWork()
    {
        const string query = @"
            select
                i.ProductName,
                i.Price,
                Floor(i.Price) as FloorPrice
            from #schema.first() o
            cross apply o.Items i";

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items = [new OrderItem { ProductName = "A", Price = 10.5m, Quantity = 1 }]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion
}
