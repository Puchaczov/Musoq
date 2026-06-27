using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class DescColumnNestedPropertyTests
{
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
        Assert.Throws<UnknownColumnOrAliasException>(() => _ = vm3.Run(TestContext.CancellationToken).Count,
            "Cannot navigate through primitive types to non-existent properties");


        var query4 = "desc #A.entities() column Self.Id";
        var vm4 = CreateAndRunVirtualMachine(query4, sources);
        Assert.Throws<ColumnMustBeAnArrayOrImplementIEnumerableException>(() => _ = vm4.Run(TestContext.CancellationToken).Count,
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
        Assert.Throws<ColumnMustBeAnArrayOrImplementIEnumerableException>(() => _ = vm1.Run(TestContext.CancellationToken).Count,
            "String properties cannot be described");


        var query2 = "desc #A.entities() column Id";
        var vm2 = CreateAndRunVirtualMachine(query2, sources);
        Assert.Throws<ColumnMustBeAnArrayOrImplementIEnumerableException>(() => _ = vm2.Run(TestContext.CancellationToken).Count,
            "Primitive properties cannot be described");
    }

    #endregion
}
