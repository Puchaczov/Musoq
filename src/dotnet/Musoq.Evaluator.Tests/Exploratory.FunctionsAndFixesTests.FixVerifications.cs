using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryFunctionsAndFixesTests
{
    #region Fix Verification: Coalesce/IfNull with null literals

    [TestMethod]
    public void WhenCoalesceWithNullAndString_ShouldReturnFallback()
    {
        const string query = @"
            select Coalesce(null, 'fallback') as Result
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("fallback", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenIfNullWithNullAndString_ShouldReturnDefault()
    {
        const string query = @"
            select IfNull(null, 'fallback') as Result
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("fallback", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenIfNullWithNullAndInteger_ShouldReturnInteger()
    {
        const string query = @"
            select IfNull(null, 42) as V
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenCoalesceWithNullAndColumnValue_ShouldReturnColumnValue()
    {
        const string query = @"
            select Coalesce(null, p.Name) as Result
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 25 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0].Values[0]);
    }

    #endregion

    #region Fix Verification: String comparison operators

    [TestMethod]
    public void WhenStringGreaterThanOrEqual_ShouldCompareCorrectly()
    {
        const string query = @"
            select p.Name
            from #schema.first() p
            where p.Name >= 'Charlie'
            order by p.Name asc";

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Charlie", Age = 25 },
            new() { Name = "Eve", Age = 20 },
            new() { Name = "Bob", Age = 35 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Charlie", table[0].Values[0]);
        Assert.AreEqual("Eve", table[1].Values[0]);
    }

    [TestMethod]
    public void WhenStringLessThan_ShouldCompareCorrectly()
    {
        const string query = @"
            select p.Name
            from #schema.first() p
            where p.Name < 'Charlie'
            order by p.Name asc";

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Charlie", Age = 25 },
            new() { Name = "Eve", Age = 20 },
            new() { Name = "Bob", Age = 35 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Alice", table[0].Values[0]);
        Assert.AreEqual("Bob", table[1].Values[0]);
    }

    [TestMethod]
    public void WhenStringGreaterThan_ShouldCompareCorrectly()
    {
        const string query = @"
            select p.Name
            from #schema.first() p
            where p.Name > 'Charlie'";

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Charlie", Age = 25 },
            new() { Name = "Eve", Age = 20 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Eve", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenStringLessThanOrEqual_ShouldCompareCorrectly()
    {
        const string query = @"
            select p.Name
            from #schema.first() p
            where p.Name <= 'Charlie'
            order by p.Name asc";

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Charlie", Age = 25 },
            new() { Name = "Eve", Age = 20 },
            new() { Name = "Bob", Age = 35 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Alice", table[0].Values[0]);
        Assert.AreEqual("Bob", table[1].Values[0]);
        Assert.AreEqual("Charlie", table[2].Values[0]);
    }

    #endregion

    #region Fix Verification: GROUP BY complex expressions

    [TestMethod]
    public void WhenGroupByArithmeticExpression_ShouldWork()
    {
        const string query = @"
            select
                p.Age / 10 * 10 as AgeDecade,
                Count(p.Name) as PersonCount
            from #schema.first() p
            group by p.Age / 10 * 10
            order by p.Age / 10 * 10 asc";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 31 },
            new() { Name = "Jane", Age = 35 },
            new() { Name = "Bob", Age = 42 },
            new() { Name = "Sue", Age = 48 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(30, table[0].Values[0]);
        Assert.AreEqual(2L, table[0].Values[1]);
        Assert.AreEqual(40, table[1].Values[0]);
        Assert.AreEqual(2L, table[1].Values[1]);
    }

    [TestMethod]
    public void WhenGroupByAddExpression_ShouldWork()
    {
        const string query = @"
            select
                p.Age + 100 as AgeShifted,
                Count(p.Name) as PersonCount
            from #schema.first() p
            group by p.Age + 100";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Jane", Age = 30 },
            new() { Name = "Bob", Age = 25 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Fix Verification: Simple CASE form

    [TestMethod]
    public void WhenSimpleCaseWithIntegerValues_ShouldWork()
    {
        const string query = @"
            select
                case p.Age
                    when 30 then 'thirty'
                    when 25 then 'twenty-five'
                    else 'other'
                end as AgeLabel
            from #schema.first() p
            order by p.Name asc";

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Bob", Age = 25 },
            new() { Name = "Charlie", Age = 40 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("thirty", table[0].Values[0]);
        Assert.AreEqual("twenty-five", table[1].Values[0]);
        Assert.AreEqual("other", table[2].Values[0]);
    }

    [TestMethod]
    public void WhenSimpleCaseWithStringValues_ShouldWork()
    {
        const string query = @"
            select
                case p.Name
                    when 'Alice' then 1
                    when 'Bob' then 2
                    else 0
                end as NameCode
            from #schema.first() p
            order by p.Name asc";

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Bob", Age = 25 },
            new() { Name = "Charlie", Age = 40 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(1, table[0].Values[0]);
        Assert.AreEqual(2, table[1].Values[0]);
        Assert.AreEqual(0, table[2].Values[0]);
    }

    #endregion
}
