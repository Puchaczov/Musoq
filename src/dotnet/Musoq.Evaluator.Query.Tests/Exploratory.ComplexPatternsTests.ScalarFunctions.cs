using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryComplexPatternsTests
{
    [TestMethod]
    public void Explore87_CastFunction_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Name,\n"
            + "                ToDecimal(p.Age) * 1.5 as AgeMultiplied\n"
            + "            from #schema.first() p"
        );

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("AgeMultiplied", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", 45m]);
    }

    [TestMethod]
    public void Explore88_SubstringFunction_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Name,\n"
            + "                Substring(p.Name, 0, 2) as FirstTwo\n"
            + "            from #schema.first() p"
        );

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("FirstTwo", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "Jo"]);
    }

    [TestMethod]
    public void Explore89_ReplaceFunction_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                t.Value,\n"
            + "                Replace(t.Value, 'a', 'X') as Replaced\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Tags t"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["apple", "banana"],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(string)), ("Replaced", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["apple", "Xpple"], ["banana", "bXnXnX"]);
    }

    [TestMethod]
    public void Explore90_TrimFunctions_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Name,\n"
            + "                Trim(p.Name) as Trimmed,\n"
            + "                TrimStart(p.Name) as LeftTrimmed,\n"
            + "                TrimEnd(p.Name) as RightTrimmed\n"
            + "            from #schema.first() p"
        );

        var source = new List<Person>
        {
            new() { Name = "  John  ", Age = 30 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Trimmed", typeof(string)),
            ("LeftTrimmed", typeof(string)),
            ("RightTrimmed", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["  John  ", "John", "John  ", "  John"]);
    }
}
