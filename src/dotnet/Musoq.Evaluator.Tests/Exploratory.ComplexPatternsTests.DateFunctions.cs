using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryComplexPatternsTests
{
    [TestMethod]
    public void Explore77_DateFunctions_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                Year(GetDate()) as CurrentYear
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var before = System.DateTimeOffset.Now;
        var table = vm.Run(TestContext.CancellationToken);
        var after = System.DateTimeOffset.Now;

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("CurrentYear", typeof(int?)));
        var rows = table.Rows;
        Assert.HasCount(1, rows);
        var row = rows[0];
        Assert.AreEqual("John", row[0]);

        var year = (int)row[1]!;
        Assert.IsTrue(
            year == before.Year || year == after.Year,
            $"Expected current year {before.Year} or {after.Year}, but received {year}.");
    }
}
