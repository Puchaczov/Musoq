using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests for the enhanced desc column feature that supports nested property paths
///     to describe private table columns within complex types.
///     Example: desc #git.commits() column Author.Repository.MyCommits
///     Where MyCommits is a property of type CommitEntity[] on Repository.
/// </summary>
[TestClass]
public class DescColumnNestedPropertyTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

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

    #region Error Handling Tests

    /// <summary>
    ///     Tests that an exception is thrown when the first part of the path doesn't exist as a column.
    /// </summary>
    [TestMethod]
    public void DescColumn_NestedProperty_NonExistentColumn_ShouldThrowException()
    {
        var query = "desc #A.entities() column NonExistent.Children";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var exception = Assert.Throws<UnknownColumnOrAliasException>(() => vm.Run(TestContext.CancellationToken));
        Assert.Contains("NonExistent", exception.Message, "Exception message should contain the column name");
    }

    /// <summary>
    ///     Tests that an exception is thrown when a nested property doesn't exist.
    /// </summary>
    [TestMethod]
    public void DescColumn_NestedProperty_NonExistentNestedProperty_ShouldThrowException()
    {
        var query = "desc #A.entities() column Self.NonExistentProperty";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var exception = Assert.Throws<UnknownColumnOrAliasException>(() => vm.Run(TestContext.CancellationToken));
        Assert.Contains("NonExistentProperty", exception.Message, "Exception message should contain the property name");
    }

    /// <summary>
    ///     Tests that an exception is thrown when trying to access a property on a primitive type.
    /// </summary>
    [TestMethod]
    public void DescColumn_NestedProperty_PropertyOnPrimitive_ShouldThrowException()
    {
        var query = "desc #A.entities() column Id.Something";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);


        Assert.Throws<UnknownColumnOrAliasException>(() => vm.Run(TestContext.CancellationToken));
    }

    /// <summary>
    ///     Tests describing a non-array complex type (for exploratory navigation).
    ///     This enables the workflow: desc #schema.method() column Self
    ///     which shows properties of Self, allowing you to discover Children, etc.
    /// </summary>
    [TestMethod]
    public void DescColumn_ComplexTypeNotArray_ShouldDescribeTypeProperties()
    {
        var query = "desc #A.entities() column Self";

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

        Assert.IsGreaterThan(0, table.Count, "Should describe the complex type");

        var columnNames = table.Select(row => (string)row[0]).ToList();


        Assert.IsTrue(columnNames.Any(c => c.Equals("Name", StringComparison.OrdinalIgnoreCase)),
            "Should show Name property");
        Assert.IsTrue(columnNames.Any(c => c.Equals("Children", StringComparison.OrdinalIgnoreCase)),
            "Should show Children array property for further exploration");
        Assert.IsTrue(columnNames.Any(c => c.Equals("Array", StringComparison.OrdinalIgnoreCase)),
            "Should show Array property for further exploration");
    }

    /// <summary>
    ///     Tests that primitive/string types cannot be described (they have no meaningful properties).
    /// </summary>
    [TestMethod]
    public void DescColumn_PrimitiveType_ShouldThrowException()
    {
        var query = "desc #A.entities() column Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);


        Assert.Throws<ColumnMustBeAnArrayOrImplementIEnumerableException>(() => vm.Run(TestContext.CancellationToken));
    }

    /// <summary>
    ///     Tests that an exception is thrown for intermediate property that is not a complex type.
    /// </summary>
    [TestMethod]
    public void DescColumn_NestedProperty_IntermediatePropertyIsPrimitive_ShouldThrowException()
    {
        var query = "desc #A.entities() column Name.Length.Something";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);


        Assert.Throws<UnknownColumnOrAliasException>(() => vm.Run(TestContext.CancellationToken));
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

    #region Unit Tests for EvaluationHelper

    /// <summary>
    ///     Unit test for GetSpecificColumnDescription with nested property path.
    /// </summary>
    [TestMethod]
    public void GetSpecificColumnDescription_WithNestedPropertyPath_ShouldWork()
    {
        var table = new BasicEntityTable();

        var result = EvaluationHelper.GetSpecificColumnDescription(table, "Self.Children");

        Assert.IsGreaterThan(0, result.Count, "Should return rows for nested property");
        Assert.AreEqual("Children", result[0][0], "First row should show the relative property name");
    }

    /// <summary>
    ///     Unit test for GetSpecificColumnDescription with deeply nested property path.
    /// </summary>
    [TestMethod]
    public void GetSpecificColumnDescription_WithDeeplyNestedPropertyPath_ShouldWork()
    {
        var table = new BasicEntityTable();

        var result = EvaluationHelper.GetSpecificColumnDescription(table, "Self.Other.Children");

        Assert.IsGreaterThan(0, result.Count, "Should return rows for deeply nested property");
        Assert.AreEqual("Children", result[0][0], "First row should show the relative property name");
    }

    /// <summary>
    ///     Unit test for GetSpecificColumnDescription with non-existent nested property.
    /// </summary>
    [TestMethod]
    public void GetSpecificColumnDescription_WithNonExistentNestedProperty_ShouldThrowException()
    {
        var table = new BasicEntityTable();

        var exception = Assert.Throws<UnknownColumnOrAliasException>(() =>
            EvaluationHelper.GetSpecificColumnDescription(table, "Self.NonExistent"));

        Assert.Contains("NonExistent", exception.Message, "Exception message should contain the property name");
    }

    #endregion

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

    #region Exploratory Navigation Workflow Tests

    /// <summary>
    ///     Tests the exploratory workflow:
    ///     1. desc #schema.method() - shows top-level columns
    ///     2. desc #schema.method() column Self - shows properties of Self
    ///     3. desc #schema.method() column Self.Children - shows properties of Children array elements
    /// </summary>
    [TestMethod]
    public void ExploratoryWorkflow_DescribeSelf_ThenDescribeChildren()
    {
        var query1 = "desc #A.entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm1 = CreateAndRunVirtualMachine(query1, sources);
        var table1 = vm1.Run(TestContext.CancellationToken);


        var topLevelColumns = table1.Select(row => (string)row[0]).ToList();
        Assert.IsTrue(topLevelColumns.Any(c => c.Equals("Self", StringComparison.OrdinalIgnoreCase)),
            "Top-level desc should show Self column");


        var query2 = "desc #A.entities() column Self";
        var vm2 = CreateAndRunVirtualMachine(query2, sources);
        var table2 = vm2.Run(TestContext.CancellationToken);

        var selfProperties = table2.Select(row => (string)row[0]).ToList();
        Assert.IsTrue(selfProperties.Any(c => c.Equals("Children", StringComparison.OrdinalIgnoreCase)),
            "Describing Self should show Children property");
        Assert.IsTrue(selfProperties.Any(c => c.Equals("Array", StringComparison.OrdinalIgnoreCase)),
            "Describing Self should show Array property");


        var query3 = "desc #A.entities() column Self.Children";
        var vm3 = CreateAndRunVirtualMachine(query3, sources);
        var table3 = vm3.Run(TestContext.CancellationToken);

        var childrenProperties = table3.Select(row => (string)row[0]).ToList();
        Assert.IsTrue(childrenProperties.Any(c => c.Equals("Name", StringComparison.OrdinalIgnoreCase)),
            "Describing Self.Children should show Name property of BasicEntity");
        Assert.IsTrue(childrenProperties.Any(c => c.Equals("Id", StringComparison.OrdinalIgnoreCase)),
            "Describing Self.Children should show Id property of BasicEntity");
    }

    /// <summary>
    ///     Tests progressive discovery: desc column Self.Other shows that Other has a Children property.
    /// </summary>
    [TestMethod]
    public void ExploratoryWorkflow_DescribeSelfOther_ShowsChildren()
    {
        var query = "desc #A.entities() column Self.Other";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        var properties = table.Select(row => (string)row[0]).ToList();


        Assert.IsTrue(properties.Any(c => c.Equals("Children", StringComparison.OrdinalIgnoreCase)),
            "Describing Self.Other should reveal Children array property for further exploration");
    }

    /// <summary>
    ///     Tests that describing a complex type shows both primitive and complex properties,
    ///     enabling users to see which properties can be drilled into further.
    /// </summary>
    [TestMethod]
    public void ExploratoryWorkflow_ComplexType_ShowsAllPropertiesForDiscovery()
    {
        var query = "desc #A.entities() column Self";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        var properties = table.Select(row => (string)row[0]).ToList();


        Assert.IsTrue(properties.Any(c => c.Equals("Name", StringComparison.OrdinalIgnoreCase)),
            "Should show primitive Name property");
        Assert.IsTrue(properties.Any(c => c.Equals("Id", StringComparison.OrdinalIgnoreCase)),
            "Should show primitive Id property");


        Assert.IsTrue(properties.Any(c => c.Equals("Children", StringComparison.OrdinalIgnoreCase)),
            "Should show Children array for drilling down");
        Assert.IsTrue(properties.Any(c => c.Equals("Time", StringComparison.OrdinalIgnoreCase)),
            "Should show complex Time property");


        Assert.IsTrue(properties.Any(c => c.Equals("Time.Date", StringComparison.OrdinalIgnoreCase)),
            "Should show that Time has nested properties");


        Assert.IsFalse(properties.Any(c => c.StartsWith("Name.", StringComparison.OrdinalIgnoreCase)),
            "Primitive Name should not have nested properties");
    }

    /// <summary>
    ///     Tests the rule: In a property chain like Self.Something.Else:
    ///     - Intermediates can be complex objects OR private tables (if private table, extract element type)
    ///     - Final property can be: complex object (for exploration) OR private table (describe elements)
    ///     - Final property CANNOT be: primitive or string
    /// </summary>
    [TestMethod]
    public void PropertyChain_IntermediatesCanBeComplexOrArray_FinalCanBeComplexOrArray()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };


        var query1 = "desc #A.entities() column Self.Other.Children";
        var vm1 = CreateAndRunVirtualMachine(query1, sources);
        var table1 = vm1.Run(TestContext.CancellationToken);
        Assert.IsGreaterThan(0, table1.Count, "Should describe array at end of chain");


        var query2 = "desc #A.entities() column Self.Other";
        var vm2 = CreateAndRunVirtualMachine(query2, sources);
        var table2 = vm2.Run(TestContext.CancellationToken);
        Assert.IsGreaterThan(0, table2.Count, "Should describe complex object for exploration");

        var properties = table2.Select(row => (string)row[0]).ToList();
        Assert.IsTrue(properties.Any(c => c.Equals("Children", StringComparison.OrdinalIgnoreCase)),
            "Describing complex object should reveal array properties for further navigation");


        var query3 = "desc #A.entities() column Id.Something";
        var vm3 = CreateAndRunVirtualMachine(query3, sources);
        Assert.Throws<UnknownColumnOrAliasException>(() => vm3.Run(TestContext.CancellationToken),
            "Cannot navigate through primitive types to non-existent properties");


        var query4 = "desc #A.entities() column Self.Id";
        var vm4 = CreateAndRunVirtualMachine(query4, sources);
        Assert.Throws<ColumnMustBeAnArrayOrImplementIEnumerableException>(() => vm4.Run(TestContext.CancellationToken),
            "Cannot describe primitive type as final property");
    }

    /// <summary>
    ///     Tests that describing primitive/string types throws exception.
    /// </summary>
    [TestMethod]
    public void DescColumn_PrimitiveOrString_ShouldThrowException()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };


        var query1 = "desc #A.entities() column Name";
        var vm1 = CreateAndRunVirtualMachine(query1, sources);
        Assert.Throws<ColumnMustBeAnArrayOrImplementIEnumerableException>(() => vm1.Run(TestContext.CancellationToken),
            "String properties cannot be described");


        var query2 = "desc #A.entities() column Id";
        var vm2 = CreateAndRunVirtualMachine(query2, sources);
        Assert.Throws<ColumnMustBeAnArrayOrImplementIEnumerableException>(() => vm2.Run(TestContext.CancellationToken),
            "Primitive properties cannot be described");
    }

    #endregion
}
