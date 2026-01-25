using System;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Unknown;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class StarTestsWithDateTimeOffset : UnknownQueryTestsBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenStarUsedWithDateTimeOffsetColumns_ShouldIncludeThem()
    {
        // Arrange
        const string query = "table Dates {" +
                             "  Id 'System.Int32'," +
                             "  Name 'System.String'," +
                             "  CreatedAt 'System.DateTimeOffset'," +
                             "  UpdatedAt 'System.Nullable`1[System.DateTimeOffset]'" +
                             "};" +
                             "couple #test.whatever with table Dates as Dates; " +
                             "select * from Dates() order by Id";

        dynamic first = new ExpandoObject();
        first.Id = 1;
        first.Name = "Test1";
        first.CreatedAt = new DateTimeOffset(new DateTime(2023, 1, 1));
        first.UpdatedAt = new DateTimeOffset(new DateTime(2023, 1, 2));

        dynamic second = new ExpandoObject();
        second.Id = 2;
        second.Name = "Test2";
        second.CreatedAt = new DateTimeOffset(new DateTime(2023, 2, 1));
        second.UpdatedAt = (DateTimeOffset?)null;

        // Act
        var vm = CreateAndRunVirtualMachine(query,
        [
            first, second
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(4, table.Columns.Count());

        var idColumn = table.Columns.Single(c => c.ColumnName == "Id");
        var nameColumn = table.Columns.Single(c => c.ColumnName == "Name");
        var createdAtColumn = table.Columns.Single(c => c.ColumnName == "CreatedAt");
        var updatedAtColumn = table.Columns.Single(c => c.ColumnName == "UpdatedAt");

        Assert.AreEqual(typeof(int?), idColumn.ColumnType);
        Assert.AreEqual(typeof(string), nameColumn.ColumnType);
        Assert.AreEqual(typeof(DateTimeOffset), createdAtColumn.ColumnType);
        Assert.AreEqual(typeof(DateTimeOffset?), updatedAtColumn.ColumnType);

        var idIndex = table.Columns.ToList().IndexOf(idColumn);
        var nameIndex = table.Columns.ToList().IndexOf(nameColumn);
        var createdAtIndex = table.Columns.ToList().IndexOf(createdAtColumn);
        var updatedAtIndex = table.Columns.ToList().IndexOf(updatedAtColumn);

        Assert.AreEqual(1, table[0].Values[idIndex]);
        Assert.AreEqual("Test1", table[0].Values[nameIndex]);
        Assert.AreEqual(new DateTimeOffset(new DateTime(2023, 1, 1)), table[0].Values[createdAtIndex]);
        Assert.AreEqual(new DateTimeOffset(new DateTime(2023, 1, 2)), table[0].Values[updatedAtIndex]);

        Assert.AreEqual(2, table[1].Values[idIndex]);
        Assert.AreEqual("Test2", table[1].Values[nameIndex]);
        Assert.AreEqual(new DateTimeOffset(new DateTime(2023, 2, 1)), table[1].Values[createdAtIndex]);
        Assert.IsNull(table[1].Values[updatedAtIndex]);
    }
}
