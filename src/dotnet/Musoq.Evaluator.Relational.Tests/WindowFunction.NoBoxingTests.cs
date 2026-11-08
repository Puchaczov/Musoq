using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using static Musoq.Evaluator.Tests.GeneratedExecutionIlInspection;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class WindowFunctionNoBoxingTests : BasicEntityTestBase
{
    [TestMethod]
    public void GeneratedWindowComputeMethods_WhenTypedWindowQueryCompiled_ShouldNotEmitBoxOpcode()
    {
        var query = @"
            select Name,
                   RowNumber() over (partition by City order by Name) as RowNo,
                   RunningProduct(Population) over (partition by City order by Name) as Product,
                   Sum(Population) over (partition by City order by Name) as RunningSum
            from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("NYC", "Alice", 2) { Name = "Alice" },
            new BasicEntity("NYC", "Bob", 3) { Name = "Bob" },
            new BasicEntity("LA", "Diana", 4) { Name = "Diana" });
        using var vm = CreateAndRunVirtualMachine(query, sources);
        AssertWindowResults(vm, ExpectedWindowRows.Product);

        var boxedMethods = GetGeneratedComputeMethods(vm)
            .Where(ContainsBoxOpcode)
            .Select(static method => method.Name)
            .ToArray();

        Assert.IsEmpty(
            boxedMethods,
            $"Generated window compute methods should not emit IL box opcodes: {string.Join(", ", boxedMethods)}");
    }

    [TestMethod]
    public void GeneratedWindowComputeMethods_WhenIntegerKeyWindowQueryCompiled_ShouldNotEmitBoxOpcode()
    {
        var query = @"
            select Name,
                   RowNumber() over (partition by Id order by Id) as RowNo,
                   Sum(Population) over (partition by Id order by Id) as RunningSum
            from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("NYC", "Alice", 2) { Name = "Alice", Id = 1 },
            new BasicEntity("NYC", "Bob", 3) { Name = "Bob", Id = 1 },
            new BasicEntity("LA", "Diana", 4) { Name = "Diana", Id = 2 });
        using var vm = CreateAndRunVirtualMachine(query, sources);
        AssertWindowResults(vm, ExpectedWindowRows.Peers);

        var boxedMethods = GetGeneratedComputeMethods(vm)
            .Where(ContainsBoxOpcode)
            .Select(static method => method.Name)
            .ToArray();

        Assert.IsEmpty(
            boxedMethods,
            $"Generated integer-key window compute methods should not emit IL box opcodes: {string.Join(", ", boxedMethods)}");
    }

    [TestMethod]
    public void GeneratedWindowComputeMethods_WhenNullableKeyWindowQueryCompiled_ShouldNotEmitBoxOpcode()
    {
        var query = @"
            select Name,
                   RowNumber() over (partition by NullableValue order by NullableValue) as RowNo,
                   Sum(Population) over (partition by NullableValue order by NullableValue) as RunningSum
            from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("NYC", "Alice", 2) { Name = "Alice", NullableValue = 1 },
            new BasicEntity("NYC", "Bob", 3) { Name = "Bob", NullableValue = 1 },
            new BasicEntity("LA", "Diana", 4) { Name = "Diana", NullableValue = null });
        using var vm = CreateAndRunVirtualMachine(query, sources);
        AssertWindowResults(vm, ExpectedWindowRows.Peers);

        var boxedMethods = GetGeneratedComputeMethods(vm)
            .Where(ContainsBoxOpcode)
            .Select(static method => method.Name)
            .ToArray();

        Assert.IsEmpty(
            boxedMethods,
            $"Generated nullable-key window compute methods should not emit IL box opcodes: {string.Join(", ", boxedMethods)}");
    }

    [TestMethod]
    public void GeneratedWindowComputeMethods_WhenMultiColumnMixedDirectionWindowQueryCompiled_ShouldNotEmitBoxOpcode()
    {
        var query = @"
            select Name,
                   RowNumber() over (partition by City order by City asc, Id desc) as RowNo,
                   Sum(Population) over (partition by City order by City asc, Id desc) as RunningSum
            from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("NYC", "Alice", 2) { Name = "Alice", Id = 5 },
            new BasicEntity("NYC", "Bob", 3) { Name = "Bob", Id = 3 },
            new BasicEntity("LA", "Diana", 4) { Name = "Diana", Id = 9 });
        using var vm = CreateAndRunVirtualMachine(query, sources);
        AssertWindowResults(vm, ExpectedWindowRows.Ordered);

        var boxedMethods = GetGeneratedComputeMethods(vm)
            .Where(ContainsBoxOpcode)
            .Select(static method => method.Name)
            .ToArray();

        Assert.IsEmpty(
            boxedMethods,
            $"Generated multi-column mixed-direction window compute methods should not emit IL box opcodes: {string.Join(", ", boxedMethods)}");
    }

    private enum ExpectedWindowRows { Ordered, Peers, Product }

    private static void AssertWindowResults(CompiledQuery query, ExpectedWindowRows expected)
    {
        using var table = query.Run();
        var rows = table.ToArray();
        Assert.HasCount(3, rows);
        var names = rows.Select(row => row.GetValue<string>(0)).ToArray();
        CollectionAssert.AreEquivalent(new[] { "Alice", "Bob", "Diana" }, names);
        var sumColumn = expected == ExpectedWindowRows.Product ? 3 : 2;
        var expectedColumns = expected == ExpectedWindowRows.Product
            ? new[] { ("Name", typeof(string)), ("RowNo", typeof(long)), ("Product", typeof(decimal)), ("RunningSum", typeof(decimal?)) }
            : new[] { ("Name", typeof(string)), ("RowNo", typeof(long)), ("RunningSum", typeof(decimal?)) };
        TableMaterializationTestHelper.AssertColumns(table, expectedColumns);
        var byName = rows.ToDictionary(row => row.GetValue<string>(0), StringComparer.Ordinal);
        Assert.AreEqual(expected == ExpectedWindowRows.Peers ? 5m : 2m, byName["Alice"].GetValue<decimal?>(sumColumn));
        Assert.AreEqual(5m, byName["Bob"].GetValue<decimal?>(sumColumn));
        Assert.AreEqual(4m, byName["Diana"].GetValue<decimal?>(sumColumn));
        CollectionAssert.AreEquivalent(new[] { 1L, 2L }, new[] { byName["Alice"].GetValue<long>(1), byName["Bob"].GetValue<long>(1) });
        Assert.AreEqual(1L, byName["Diana"].GetValue<long>(1));
        if (expected == ExpectedWindowRows.Product)
        {
            Assert.AreEqual(2m, byName["Alice"].GetValue<decimal>(2));
            Assert.AreEqual(6m, byName["Bob"].GetValue<decimal>(2));
            Assert.AreEqual(4m, byName["Diana"].GetValue<decimal>(2));
        }
    }

}
