using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class DescColumnNestedPropertyTests
{
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

        var exception = Assert.Throws<UnknownColumnOrAliasException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
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

        var exception = Assert.Throws<UnknownColumnOrAliasException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
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


        Assert.Throws<UnknownColumnOrAliasException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
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


        Assert.Throws<ColumnMustBeAnArrayOrImplementIEnumerableException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
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


        Assert.Throws<UnknownColumnOrAliasException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
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
}
