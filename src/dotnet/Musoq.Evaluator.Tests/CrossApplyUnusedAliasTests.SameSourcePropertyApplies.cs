using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class CrossApplyUnusedAliasTests
{
    #region Multiple Property Cross Applies - Dictionary Key Issue

    /// <summary>
    ///     Test multiple cross applies on different properties from same source.
    ///     Pattern: m.A a cross apply m.B b - where both A and B are properties of m.
    /// </summary>
    [TestMethod]
    public void CrossApply_TwoPropertiesFromSameSource_ShouldWork()
    {
        const string query = @"
            select 1
            from #schema.first() m
            cross apply m.Values1 a
            cross apply m.Values2 b";

        var firstSource = new List<CrossApplyMultiProperty>
        {
            new() { Name = "Test1", Values1 = [1, 2], Values2 = [10, 20] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("1", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1], [1], [1], [1]);
    }

    /// <summary>
    ///     Test multiple cross applies on properties - selecting from applied aliases.
    /// </summary>
    [TestMethod]
    public void CrossApply_TwoPropertiesFromSameSource_SelectFromBoth_ShouldWork()
    {
        const string query = @"
            select a.Value, b.Value
            from #schema.first() m
            cross apply m.Values1 a
            cross apply m.Values2 b";

        var firstSource = new List<CrossApplyMultiProperty>
        {
            new() { Name = "Test1", Values1 = [1, 2], Values2 = [10, 20] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.Value", typeof(int)), ("b.Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [1, 10], [1, 20], [2, 10], [2, 20]);
    }

    /// <summary>
    ///     Test multiple cross applies on properties - selecting only from second applied alias.
    /// </summary>
    [TestMethod]
    public void CrossApply_TwoPropertiesFromSameSource_SelectOnlyFromSecond_ShouldWork()
    {
        const string query = @"
            select b.Value
            from #schema.first() m
            cross apply m.Values1 a
            cross apply m.Values2 b";

        var firstSource = new List<CrossApplyMultiProperty>
        {
            new() { Name = "Test1", Values1 = [1, 2], Values2 = [10, 20] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("b.Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [10], [10], [20], [20]);
    }

    /// <summary>
    ///     Test multiple cross applies on properties - original source alias used in select.
    /// </summary>
    [TestMethod]
    public void CrossApply_TwoPropertiesFromSameSource_SelectFromOriginal_ShouldWork()
    {
        const string query = @"
            select m.Name, a.Value, b.Value
            from #schema.first() m
            cross apply m.Values1 a
            cross apply m.Values2 b";

        var firstSource = new List<CrossApplyMultiProperty>
        {
            new() { Name = "Test1", Values1 = [1, 2], Values2 = [10, 20] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("m.Name", typeof(string)), ("a.Value", typeof(int)), ("b.Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Test1", 1, 10], ["Test1", 1, 20], ["Test1", 2, 10], ["Test1", 2, 20]);
    }

    /// <summary>
    ///     Test three cross applies on properties from same source.
    /// </summary>
    [TestMethod]
    public void CrossApply_ThreePropertiesFromSameSource_ShouldWork()
    {
        const string query = @"
            select a.Value, b.Value, c.Value
            from #schema.first() m
            cross apply m.Values1 a
            cross apply m.Values2 b
            cross apply m.Values3 c";

        var firstSource = new List<CrossApplyMultiProperty>
        {
            new() { Name = "Test1", Values1 = [1], Values2 = [10], Values3 = [100] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Value", typeof(int)), ("b.Value", typeof(int)), ("c.Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 10, 100]);
    }

    /// <summary>
    ///     Test CTE with multiple property cross applies from same source.
    /// </summary>
    [TestMethod]
    public void Cte_CrossApply_TwoPropertiesFromSameSource_ShouldWork()
    {
        const string query = @"
            with cte as (
                select m.Name as Name, m.Values1 as Values1, m.Values2 as Values2
                from #schema.first() m
            )
            select c.Name, a.Value, b.Value
            from cte c
            cross apply c.Values1 a
            cross apply c.Values2 b";

        var firstSource = new List<CrossApplyMultiProperty>
        {
            new() { Name = "Test1", Values1 = [1, 2], Values2 = [10, 20] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("c.Name", typeof(string)), ("a.Value", typeof(int)), ("b.Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Test1", 1, 10], ["Test1", 1, 20], ["Test1", 2, 10], ["Test1", 2, 20]);
    }

    /// <summary>
    ///     Test cross apply on property, then method call on same source.
    /// </summary>
    [TestMethod]
    public void CrossApply_PropertyThenMethodOnSameSource_ShouldWork()
    {
        const string query = @"
            select a.Value, b.Value
            from #schema.first() m
            cross apply m.Values1 a
            cross apply m.Split(m.Name, ',') b";

        var firstSource = new List<CrossApplyMultiProperty>
        {
            new() { Name = "A,B,C", Values1 = [1, 2], Values2 = [10, 20], Values3 = [100] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Value", typeof(int)), ("b.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [1, "A"], [1, "B"], [1, "C"], [2, "A"], [2, "B"], [2, "C"]);
    }

    /// <summary>
    ///     Test cross apply chain: source -> property -> property on applied result.
    /// </summary>
    [TestMethod]
    public void CrossApply_ChainedPropertyAccess_ShouldWork()
    {
        const string query = @"
            select m.Name, a.Value
            from #schema.first() m
            cross apply m.NestedValues a";

        var firstSource = new List<CrossApplyNestedProperty>
        {
            new() { Name = "Test1", NestedValues = [new NestedValue { Value = 1 }, new NestedValue { Value = 2 }] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("m.Name", typeof(string)), ("a.Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Test1", 1], ["Test1", 2]);
    }

    /// <summary>
    ///     Test outer apply on two properties from same source.
    /// </summary>
    [TestMethod]
    public void OuterApply_TwoPropertiesFromSameSource_ShouldWork()
    {
        const string query = @"
            select m.Name, a.Value, b.Value
            from #schema.first() m
            outer apply m.Values1 a
            outer apply m.Values2 b";

        var firstSource = new List<CrossApplyMultiProperty>
        {
            new() { Name = "Test1", Values1 = [1, 2], Values2 = [10, 20] },
            new() { Name = "Test2", Values1 = [], Values2 = [30] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("m.Name", typeof(string)), ("a.Value", typeof(int?)), ("b.Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Test1", 1, 10], ["Test1", 1, 20], ["Test1", 2, 10], ["Test1", 2, 20],
            ["Test2", null, 30]);
    }

    /// <summary>
    ///     Test mixed cross apply and outer apply on properties from same source.
    /// </summary>
    [TestMethod]
    public void CrossApply_ThenOuterApply_OnPropertiesFromSameSource_ShouldWork()
    {
        const string query = @"
            select m.Name, a.Value, b.Value
            from #schema.first() m
            cross apply m.Values1 a
            outer apply m.Values2 b";

        var firstSource = new List<CrossApplyMultiProperty>
        {
            new() { Name = "Test1", Values1 = [1, 2], Values2 = [] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("m.Name", typeof(string)), ("a.Value", typeof(int)), ("b.Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Test1", 1, null], ["Test1", 2, null]);
    }

    #endregion
}
