using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryFunctionsAndFixesTests
{
    #region Fix Verification: Coalesce/IfNull with null literals

    [TestMethod]
    public void WhenCoalesceWithNullAndString_ShouldReturnFallback()
    {
        const string query = (
            "\n" + "            select Coalesce(null, 'fallback') as Result\n" + "            from #schema.first() p"
        );

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("fallback", table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void WhenIfNullWithNullAndString_ShouldReturnDefault()
    {
        const string query = (
            "\n" + "            select IfNull(null, 'fallback') as Result\n" + "            from #schema.first() p"
        );

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("fallback", table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void WhenIfNullWithNullAndInteger_ShouldReturnInteger()
    {
        const string query = (
            "\n" + "            select IfNull(null, 42) as V\n" + "            from #schema.first() p"
        );

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void WhenCoalesceWithNullAndColumnValue_ShouldReturnColumnValue()
    {
        const string query = (
            "\n" + "            select Coalesce(null, p.Name) as Result\n" + "            from #schema.first() p"
        );

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 25 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0].ReadValues()[0]);
    }

    #endregion

    #region Fix Verification: String comparison operators

    [TestMethod]
    public void WhenStringGreaterThanOrEqual_ShouldCompareCorrectly()
    {
        const string query = (
            "\n"
            + "            select p.Name\n"
            + "            from #schema.first() p\n"
            + "            where p.Name >= 'Charlie'\n"
            + "            order by p.Name asc"
        );

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Charlie", Age = 25 },
            new() { Name = "Eve", Age = 20 },
            new() { Name = "Bob", Age = 35 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Charlie", table[0].ReadValues()[0]);
        Assert.AreEqual("Eve", table[1].ReadValues()[0]);
    }

    [TestMethod]
    public void WhenStringLessThan_ShouldCompareCorrectly()
    {
        const string query = (
            "\n"
            + "            select p.Name\n"
            + "            from #schema.first() p\n"
            + "            where p.Name < 'Charlie'\n"
            + "            order by p.Name asc"
        );

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Charlie", Age = 25 },
            new() { Name = "Eve", Age = 20 },
            new() { Name = "Bob", Age = 35 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Alice", table[0].ReadValues()[0]);
        Assert.AreEqual("Bob", table[1].ReadValues()[0]);
    }

    [TestMethod]
    public void WhenStringGreaterThan_ShouldCompareCorrectly()
    {
        const string query = (
            "\n"
            + "            select p.Name\n"
            + "            from #schema.first() p\n"
            + "            where p.Name > 'Charlie'"
        );

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Charlie", Age = 25 },
            new() { Name = "Eve", Age = 20 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Eve", table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void WhenStringLessThanOrEqual_ShouldCompareCorrectly()
    {
        const string query = (
            "\n"
            + "            select p.Name\n"
            + "            from #schema.first() p\n"
            + "            where p.Name <= 'Charlie'\n"
            + "            order by p.Name asc"
        );

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Charlie", Age = 25 },
            new() { Name = "Eve", Age = 20 },
            new() { Name = "Bob", Age = 35 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Alice", table[0].ReadValues()[0]);
        Assert.AreEqual("Bob", table[1].ReadValues()[0]);
        Assert.AreEqual("Charlie", table[2].ReadValues()[0]);
    }

    #endregion

    #region Fix Verification: GROUP BY complex expressions

    [TestMethod]
    public void WhenGroupByArithmeticExpression_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Age / 10 * 10 as AgeDecade,\n"
            + "                Count(p.Name) as PersonCount\n"
            + "            from #schema.first() p\n"
            + "            group by p.Age / 10 * 10\n"
            + "            order by p.Age / 10 * 10 asc"
        );

        var source = new List<Person>
        {
            new() { Name = "John", Age = 31 },
            new() { Name = "Jane", Age = 35 },
            new() { Name = "Bob", Age = 42 },
            new() { Name = "Sue", Age = 48 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(30, table[0].ReadValues()[0]);
        Assert.AreEqual(2L, table[0].ReadValues()[1]);
        Assert.AreEqual(40, table[1].ReadValues()[0]);
        Assert.AreEqual(2L, table[1].ReadValues()[1]);
    }

    [TestMethod]
    public void WhenGroupByAddExpression_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Age + 100 as AgeShifted,\n"
            + "                Count(p.Name) as PersonCount\n"
            + "            from #schema.first() p\n"
            + "            group by p.Age + 100"
        );

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Jane", Age = 30 },
            new() { Name = "Bob", Age = 25 },
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
        const string query = (
            "\n"
            + "            select\n"
            + "                case p.Age\n"
            + "                    when 30 then 'thirty'\n"
            + "                    when 25 then 'twenty-five'\n"
            + "                    else 'other'\n"
            + "                end as AgeLabel\n"
            + "            from #schema.first() p\n"
            + "            order by p.Name asc"
        );

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Bob", Age = 25 },
            new() { Name = "Charlie", Age = 40 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("thirty", table[0].ReadValues()[0]);
        Assert.AreEqual("twenty-five", table[1].ReadValues()[0]);
        Assert.AreEqual("other", table[2].ReadValues()[0]);
    }

    [TestMethod]
    public void WhenSimpleCaseWithStringValues_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                case p.Name\n"
            + "                    when 'Alice' then 1\n"
            + "                    when 'Bob' then 2\n"
            + "                    else 0\n"
            + "                end as NameCode\n"
            + "            from #schema.first() p\n"
            + "            order by p.Name asc"
        );

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Bob", Age = 25 },
            new() { Name = "Charlie", Age = 40 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(1, table[0].ReadValues()[0]);
        Assert.AreEqual(2, table[1].ReadValues()[0]);
        Assert.AreEqual(0, table[2].ReadValues()[0]);
    }

    #endregion
}
