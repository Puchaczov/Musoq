using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryComplexPatternsTests
{
    [TestMethod]
    public void Explore87_CastFunction_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                ToDecimal(p.Age) * 1.5 as AgeMultiplied
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
    public void Explore88_SubstringFunction_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                Substring(p.Name, 0, 2) as FirstTwo
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
    public void Explore89_ReplaceFunction_ShouldWork()
    {
        const string query = @"
            select
                t.Value,
                Replace(t.Value, 'a', 'X') as Replaced
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["apple", "banana"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }



    [TestMethod]
    public void Explore90_TrimFunctions_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                Trim(p.Name) as Trimmed,
                TrimStart(p.Name) as LeftTrimmed,
                TrimEnd(p.Name) as RightTrimmed
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "  John  ", Age = 30 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

}
