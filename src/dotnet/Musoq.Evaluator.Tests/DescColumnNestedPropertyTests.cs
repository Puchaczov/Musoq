using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests for the enhanced desc column feature that supports nested property paths
///     to describe private table columns within complex types.
///     Example: desc #git.commits() column Author.Repository.MyCommits
///     Where MyCommits is a property of type CommitEntity[] on Repository.
/// </summary>
[TestClass]
public partial class DescColumnNestedPropertyTests : BasicEntityTestBase
{

    #region Dictionary/IEnumerable Nested Property Tests

    /// <summary>
    ///     Tests describing a nested Dictionary property (implements IEnumerable).
    /// </summary>
    [TestMethod]
    public void DescColumn_NestedDictionaryProperty_ShouldWork()
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

        Assert.IsGreaterThan(0, table.Count, "Dictionary implements IEnumerable and should work");

        var columnName = (string)table[0][0];
        Assert.AreEqual("Dictionary", columnName, "Should show the relative property name");
    }

    #endregion

    #region Single Level Nested Property Tests

    /// <summary>
    ///     Tests describing a nested property that is an array (private table).
    ///     Self is a BasicEntity, and Children is a BasicEntity[] on it.
    /// </summary>
    [TestMethod]
    public void DescColumn_SingleLevelNestedProperty_ShouldDescribeNestedArrayType()
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

        Assert.AreEqual(3, table.Columns.Count(), "Should have 3 columns: Name, Index, Type");
        Assert.IsGreaterThan(0, table.Count, "Should return at least one row for the nested array property");

        var firstColumnName = (string)table[0][0];
        Assert.AreEqual("Children", firstColumnName, "First row should show the relative property name");
    }

    /// <summary>
    ///     Tests that describing a nested property that is an array (private table)
    ///     returns ALL properties of the element type (including primitives and strings),
    ///     but only complex types are navigable for further drilling down.
    /// </summary>
    [TestMethod]
    public void DescColumn_NestedArrayProperty_ShouldContainAllPropertiesIncludingPrimitives()
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

        var columnNames = table.Select(row => (string)row[0]).ToList();


        Assert.AreEqual("Children", columnNames[0], "First row should be the base element type");


        var expectedProperties = new[]
        {
            "Name",
            "City",
            "Country",
            "Month",

            "Id",

            "Self",
            "Other",
            "Array",
            "Children",
            "Dictionary",
            "Time",
            "Money",
            "Population",
            "NullableValue"
        };

        foreach (var expected in expectedProperties)
            Assert.IsTrue(
                columnNames.Any(c => c.Equals(expected, StringComparison.OrdinalIgnoreCase)),
                $"Expected property '{expected}' not found. Found: {string.Join(", ", columnNames)}");
    }

    /// <summary>
    ///     Tests that complex properties can be navigated further (nested drilling),
    ///     while primitive/string properties are leaf nodes.
    /// </summary>
    [TestMethod]
    public void DescColumn_NestedArrayProperty_ComplexPropertiesShouldBeNavigable()
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

        var columnNames = table.Select(row => (string)row[0]).ToList();


        var expectedNestedComplexProperties = new[]
        {
            "Time.Date",
            "Time.TimeOfDay",
            "Dictionary.Keys",
            "Dictionary.Values"
        };

        foreach (var expected in expectedNestedComplexProperties)
            Assert.IsTrue(
                columnNames.Any(c => c.Equals(expected, StringComparison.OrdinalIgnoreCase)),
                $"Expected nested complex property '{expected}' not found. Found: {string.Join(", ", columnNames)}");


        Assert.IsFalse(
            columnNames.Any(c => c.StartsWith("Id.", StringComparison.OrdinalIgnoreCase)),
            "Primitive property 'Id' should not have nested properties");
        Assert.IsFalse(
            columnNames.Any(c => c.StartsWith("Name.", StringComparison.OrdinalIgnoreCase)),
            "String property 'Name' should not have nested properties");
    }

    /// <summary>
    ///     Tests describing a nested property that is an array of primitives.
    ///     Self is a BasicEntity, and Array is an int[] on it.
    /// </summary>
    [TestMethod]
    public void DescColumn_SingleLevelNestedPrimitiveArray_ShouldDescribeElementType()
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

        Assert.AreEqual(1, table.Count, "Should return one row for primitive array element type");

        var columnName = (string)table[0][0];
        Assert.AreEqual("Array", columnName, "Should show the relative property name");

        var typeName = (string)table[0][2];
        Assert.Contains("Int32", typeName, "Array element should be Int32 type");
    }

    #endregion

    #region Multi-Level Nested Property Tests

    /// <summary>
    ///     Tests describing a deeply nested property (two levels: Self.Other.Children).
    /// </summary>
    [TestMethod]
    public void DescColumn_TwoLevelNestedProperty_ShouldDescribeNestedArrayType()
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

        Assert.IsGreaterThan(0, table.Count, "Should return rows for the deeply nested array property");

        var firstColumnName = (string)table[0][0];
        Assert.AreEqual("Children", firstColumnName, "First row should show the relative property name");
    }

    /// <summary>
    ///     Tests describing a three-level nested property (Self.Other.Self.Array).
    /// </summary>
    [TestMethod]
    public void DescColumn_ThreeLevelNestedProperty_ShouldDescribeNestedArrayType()
    {
        var query = "desc #A.entities() column Self.Other.Self.Array";

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

        Assert.AreEqual(1, table.Count, "Should return one row for primitive array");

        var columnName = (string)table[0][0];
        Assert.AreEqual("Array", columnName, "Should show the relative property name");
    }

    #endregion

    #region Case Sensitivity Tests

    /// <summary>
    ///     Tests that nested property path is case-insensitive for the first part (column name).
    /// </summary>
    [TestMethod]
    public void DescColumn_NestedProperty_CaseInsensitiveColumnName_ShouldWork()
    {
        var query = "desc #A.entities() column self.Children";

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

        Assert.IsGreaterThan(0, table.Count, "Should find column with case-insensitive match");
    }

    /// <summary>
    ///     Tests that nested property path is case-insensitive for nested properties.
    /// </summary>
    [TestMethod]
    public void DescColumn_NestedProperty_CaseInsensitiveNestedProperty_ShouldWork()
    {
        var query = "desc #A.entities() column Self.children";

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

        Assert.IsGreaterThan(0, table.Count, "Should find nested property with case-insensitive match");
    }

    #endregion

}
