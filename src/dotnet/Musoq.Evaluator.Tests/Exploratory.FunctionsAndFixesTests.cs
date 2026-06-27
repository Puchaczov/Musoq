using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Exploratory tests: Functions and fix verifications (Explorations 91-110+).
/// </summary>
[TestClass]
public partial class ExploratoryFunctionsAndFixesTests
{

    #region Exploration 92: Where with multiple IN clauses

    [TestMethod]
    public void Explore92_WhereMultipleInClauses_ShouldWork()
    {
        const string query = @"
            select p.Name
            from #schema.first() p
            where p.Name in ('John', 'Jane') and p.Age in (25, 30)";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Jane", Age = 25 },
            new() { Name = "Bob", Age = 30 },
            new() { Name = "Alice", Age = 35 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 95: Mixed aggregates with Where

    [TestMethod]
    public void Explore95_MixedAggregatesWithWhere_ShouldWork()
    {
        const string query = @"
            select
                o.OrderId,
                Count(i.ProductName) as ItemCount,
                Sum(i.Price) as TotalPrice
            from #schema.first() o
            cross apply o.Items i
            where i.Price > 5
            group by o.OrderId";

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items =
                [
                    new OrderItem { ProductName = "Cheap", Price = 3m, Quantity = 1 },
                    new OrderItem { ProductName = "Expensive", Price = 15m, Quantity = 1 },
                    new OrderItem { ProductName = "Medium", Price = 8m, Quantity = 1 }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Exploration 96: String comparison with case sensitivity

    [TestMethod]
    public void Explore96_StringComparison_CaseSensitive_ShouldWork()
    {
        const string query = @"
            select p.Name
            from #schema.first() p
            where p.Name = 'john'";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "john", Age = 25 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("john", table[0].Values[0]);
    }

    #endregion

    #region Exploration 110: Complex query combining multiple features

    [TestMethod]
    public void Explore110_ComplexCombinedQuery_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                ToUpper(t.Value) as TagUpper,
                p.Age * 2 as DoubleAge
            from #schema.first() p
            cross apply p.Tags t
            where p.Age >= 25 and t.Value like '%a%'
            order by t.Value asc
            take 5";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["admin", "data", "xyz"] },
            new() { Name = "Jane", Age = 20, Tags = ["admin"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.IsLessThanOrEqualTo(5, table.Count);
    }

    #endregion

}
