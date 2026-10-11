using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
    [TestMethod]
    public void TypedStateKernel_WhenRangeKeysAreVolatile_ShouldReuseTheSortingCapture()
    {
        const string query = "select Position, Sum(Value) over (order by CaptureOrderKey(Position) range between 1 preceding and current row) as Total from #schema.first() order by Position";
        NumericRow<int>[] input = [new(3, 30), new(1, 10), new(2, 20)];
        CleanupLibrary.OrderKeyCalls = 0;
        using var compiled = CreateAndRunVirtualMachine<NumericRow<int>, CleanupLibrary>(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Total", typeof(int?)));
        Assert.HasCount(3, rows);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, rows.Select(row => row.GetValue<int>(0)).ToArray());
        CollectionAssert.AreEqual(new int?[] { 10, 30, 50 }, rows.Select(row => row.GetValue<int?>(1)).ToArray());
        Assert.AreEqual(3, CleanupLibrary.OrderKeyCalls);
    }

    [TestMethod]
    public void LegacyWindowFactory_WhenCalledFromSql_ShouldBeUnavailable()
    {
        const string query = "select WindowSum() from #schema.first()";
        var error = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<NumericRow<int>, CleanupLibrary>(query, [new(1, 1)]));
        Assert.Contains("WindowSum", error.ToString());
    }

    [TestMethod]
    public void LegacyWindowContracts_ShouldBeAbsentFromTheProductionAssembly()
    {
        var assembly = typeof(LibraryBase).Assembly;
        Assert.IsNull(assembly.GetType("Musoq.Plugins.Attributes.AggregationMethodAttribute"));
        Assert.AreEqual(typeof(BindableMethodAttribute), typeof(AggregateFunctionAttribute).BaseType);
        Assert.IsNull(assembly.GetType("Musoq.Plugins.IWindowFunction"));
        Assert.IsNull(assembly.GetType("Musoq.Plugins.IWindowFunction`2"));
        Assert.IsNull(assembly.GetType("Musoq.Plugins.IWindowAggregateCapabilityProvider"));
        Assert.IsNull(typeof(LibraryBase).GetMethod("WindowSum"));
    }

    public sealed class CleanupLibrary : LibraryBase
    {
        public static int OrderKeyCalls;
        [BindableMethod, NonDeterministic]
        public static int CaptureOrderKey(int value)
        {
            Interlocked.Increment(ref OrderKeyCalls);
            return value;
        }
    }
}
