using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryComplexPatternsTests
{
    [TestMethod]
    public void Explore77_DateFunctions_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Name,\n"
            + "                Year(GetDate()) as CurrentYear\n"
            + "            from #schema.first() p"
        );

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
        }.ToArray();

        using var vm = CreateAndRunVirtualMachine<Person, FixedDateLibrary>(query, source);
        using var table = vm.Run(TestContext.CancellationToken);
        var rows = table.Rows;

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("CurrentYear", typeof(int?)));
        Assert.HasCount(1, rows);
        var row = rows[0];
        Assert.AreEqual("John", row.ReadValue(0));

        Assert.AreEqual(2031, row.ReadValue(1));
    }

    public sealed class FixedDateLibrary : LibraryBase
    {
        internal override DateTimeOffset CurrentLocalDateTime => new(2031, 7, 8, 9, 10, 11, TimeSpan.FromHours(2));

        [BindableMethod]
        [NonDeterministic]
        public new DateTimeOffset? GetDate() => base.GetDate();
    }
}
