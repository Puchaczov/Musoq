using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryQueriesAndJoinsTests
{
    #region Exploration 41: CASE expressions with Cross Apply

    [TestMethod]
    public void Explore41_CrossApply_CaseWhenInSelect_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value,
                case when t.Value = 'admin' then 'Admin User' else 'Regular User' end as UserType
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["admin", "user"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Explore41_CrossApply_CaseWhenWithNull_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                case when p.Tags is null then 'No Tags' else 'Has Tags' end as TagStatus
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

    #region Exploration 42: Multiple cross applies with different array types

    [TestMethod]
    public void Explore42_MultipleCrossApplies_DifferentArrayTypes_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value as Tag,
                a.Street
            from #schema.first() p
            cross apply p.Tags t
            cross apply p.Addresses a";

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["vip"],
                Addresses = [new Address { Street = "123 Main St", City = "NYC" }]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Exploration 43: Nested property access after cross apply

    [TestMethod]
    public void Explore43_CrossApply_NestedPropertyAccess_ShouldWork()
    {
        const string query = @"
            select
                o.OrderId,
                i.ProductName,
                i.Price * i.Quantity as LineTotal
            from #schema.first() o
            cross apply o.Items i";

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items =
                [
                    new OrderItem { ProductName = "Widget", Price = 10.00m, Quantity = 2 },
                    new OrderItem { ProductName = "Gadget", Price = 25.50m, Quantity = 1 }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 44: Case/IfNull with cross apply

    [TestMethod]
    public void Explore44_CrossApply_CaseWhenNull_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                case when a.City is null then 'Unknown' else a.City end as City
            from #schema.first() p
            outer apply p.Addresses a";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Addresses = null },
            new() { Name = "Jane", Age = 25, Addresses = [new Address { City = "NYC" }] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    #endregion

    #region Exploration 45: String functions with cross apply values

    [TestMethod]
    public void Explore45_CrossApply_StringFunctions_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                ToUpper(t.Value) as UpperTag,
                ToLower(t.Value) as LowerTag
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["hello", "world"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 49: Boolean expressions with cross apply

    [TestMethod]
    public void Explore49_CrossApply_BooleanExpression_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value,
                t.Value = 'admin' as IsAdmin
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["admin", "user"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Explore49_CrossApply_AndOrExpression_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t
            where t.Value = 'admin' or t.Value = 'vip'";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["admin", "user", "vip"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 50: Type conversion edge cases

    [TestMethod]
    public void Explore50_CrossApply_ToStringConversion_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                ToString(p.Age) as AgeString,
                p.Age + 0 as AgeNumber
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

    [TestMethod]
    public void Explore50_CrossApply_DecimalToInt_ShouldWork()
    {
        const string query = @"
            select
                i.ProductName,
                ToInt32(i.Price) as RoundedPrice
            from #schema.first() o
            cross apply o.Items i";

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items = [new OrderItem { ProductName = "Widget", Price = 10.99m, Quantity = 1 }]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion
}
