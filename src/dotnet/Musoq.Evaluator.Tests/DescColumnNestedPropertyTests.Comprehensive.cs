using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class DescColumnNestedPropertyTests
{
    #region Comprehensive Evaluator Tests

    /// <summary>
    ///     Tests that describing a private table returns correct type information for each property.
    /// </summary>
    [TestMethod]
    public void DescColumn_NestedArrayProperty_ShouldReturnCorrectTypeInformation()
    {
        var query = "desc #A.entities() column Self.Children";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        var nameRow = table.FirstOrDefault(row => ((string)row[0]).Equals("Name", StringComparison.OrdinalIgnoreCase));
        Assert.IsNotNull(nameRow, "Should have a row for Name");
        Assert.Contains("String", (string)nameRow[2], "Name property should be of type String");


        var idRow = table.FirstOrDefault(row => ((string)row[0]).Equals("Id", StringComparison.OrdinalIgnoreCase));
        Assert.IsNotNull(idRow, "Should have a row for Id");
        Assert.Contains("Int32", (string)idRow[2], "Id property should be of type Int32");


        var timeRow = table.FirstOrDefault(row => ((string)row[0]).Equals("Time", StringComparison.OrdinalIgnoreCase));
        Assert.IsNotNull(timeRow, "Should have a row for Time");
        Assert.Contains("DateTime", (string)timeRow[2], "Time property should be of type DateTime");


        var childrenRow =
            table.FirstOrDefault(row => ((string)row[0]).Equals("Children", StringComparison.OrdinalIgnoreCase));
        Assert.IsNotNull(childrenRow, "Should have a row for Children");
        Assert.Contains("BasicEntity", (string)childrenRow[2], "Children property should be of type BasicEntity[]");
    }

    /// <summary>
    ///     Tests describing a two-level nested private table returns all properties of the nested element.
    /// </summary>
    [TestMethod]
    public void DescColumn_TwoLevelNestedPrivateTable_ShouldContainAllProperties()
    {
        var query = "desc #A.entities() column Self.Other.Children";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        var columnNames = table.Select(row => (string)row[0]).ToList();


        var expectedProperties = new[]
        {
            "Children",
            "Name",
            "City",
            "Id",
            "Time",
            "Children"
        };

        foreach (var expected in expectedProperties)
            Assert.IsTrue(
                columnNames.Any(c => c.Equals(expected, StringComparison.OrdinalIgnoreCase)),
                $"Expected property '{expected}' not found. Found: {string.Join(", ", columnNames)}");
    }

    /// <summary>
    ///     Tests that the desc output has correct table structure with Name, Index, Type columns.
    /// </summary>
    [TestMethod]
    public void DescColumn_NestedPrivateTable_ShouldHaveCorrectTableStructure()
    {
        var query = "desc #A.entities() column Self.Children";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(3, table.Columns.Count(), "Should have exactly 3 columns");

        var columns = table.Columns.ToList();
        Assert.AreEqual("Name", columns[0].ColumnName, "First column should be 'Name'");
        Assert.AreEqual("Index", columns[1].ColumnName, "Second column should be 'Index'");
        Assert.AreEqual("Type", columns[2].ColumnName, "Third column should be 'Type'");


        Assert.AreEqual(typeof(string), columns[0].ColumnType, "Name column should be string");
        Assert.AreEqual(typeof(int), columns[1].ColumnType, "Index column should be int");
        Assert.AreEqual(typeof(string), columns[2].ColumnType, "Type column should be string");
    }

    /// <summary>
    ///     Tests that primitive arrays (e.g., int[]) are described correctly with element type.
    /// </summary>
    [TestMethod]
    public void DescColumn_PrimitiveArray_ShouldShowElementType()
    {
        var query = "desc #A.entities() column Self.Array";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(1, table.Count, "Primitive array should have exactly 1 row for element type");

        var columnName = (string)table[0][0];
        Assert.AreEqual("Array", columnName, "Should show the relative property name");

        var typeName = (string)table[0][2];
        Assert.Contains("Int32", typeName, "Should show Int32 as the element type");
    }

    /// <summary>
    ///     Tests describing a Dictionary property (IEnumerable of KeyValuePair).
    /// </summary>
    [TestMethod]
    public void DescColumn_DictionaryProperty_ShouldShowKeyValuePairProperties()
    {
        var query = "desc #A.entities() column Self.Dictionary";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        var columnNames = table.Select(row => (string)row[0]).ToList();


        Assert.IsTrue(
            columnNames.Any(c => c.Equals("Key", StringComparison.OrdinalIgnoreCase)),
            $"Should have Key property. Found: {string.Join(", ", columnNames)}");
        Assert.IsTrue(
            columnNames.Any(c => c.Equals("Value", StringComparison.OrdinalIgnoreCase)),
            $"Should have Value property. Found: {string.Join(", ", columnNames)}");
    }

    /// <summary>
    ///     Tests that the Index column in desc output refers to the original column index.
    /// </summary>
    [TestMethod]
    public void DescColumn_NestedPrivateTable_IndexShouldReferToOriginalColumn()
    {
        var query = "desc #A.entities() column Self.Children";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        var indices = table.Select(row => (int)row[1]).Distinct().ToList();


        Assert.HasCount(1, indices, "All nested properties should reference the same original column index");
    }

    #endregion
}
