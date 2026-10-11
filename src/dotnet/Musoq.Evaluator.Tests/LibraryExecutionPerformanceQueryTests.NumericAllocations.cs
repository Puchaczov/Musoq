using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionPerformanceQueryTests
{
    [TestMethod]
    public void ByteSum_ShouldExecuteTypedQueryAndAllocateNoKernelBoxes()
        => AssertNumericKernel<byte>();

    [TestMethod]
    public void SByteSum_ShouldExecuteTypedQueryAndAllocateNoKernelBoxes()
        => AssertNumericKernel<sbyte>();

    [TestMethod]
    public void Int16Sum_ShouldExecuteTypedQueryAndAllocateNoKernelBoxes()
        => AssertNumericKernel<short>();

    [TestMethod]
    public void UInt16Sum_ShouldExecuteTypedQueryAndAllocateNoKernelBoxes()
        => AssertNumericKernel<ushort>();

    [TestMethod]
    public void Int32Sum_ShouldExecuteTypedQueryAndAllocateNoKernelBoxes()
        => AssertNumericKernel<int>();

    [TestMethod]
    public void UInt32Sum_ShouldExecuteTypedQueryAndAllocateNoKernelBoxes()
        => AssertNumericKernel<uint>();

    [TestMethod]
    public void Int64Sum_ShouldExecuteTypedQueryAndAllocateNoKernelBoxes()
        => AssertNumericKernel<long>();

    [TestMethod]
    public void UInt64Sum_ShouldExecuteTypedQueryAndAllocateNoKernelBoxes()
        => AssertNumericKernel<ulong>();

    [TestMethod]
    public void DecimalSum_ShouldExecuteTypedQueryAndAllocateNoKernelBoxes()
        => AssertNumericKernel<decimal>();

    [TestMethod]
    public void SingleSum_ShouldExecuteTypedQueryAndAllocateNoKernelBoxes()
        => AssertNumericKernel<float>();

    [TestMethod]
    public void DoubleSum_ShouldExecuteTypedQueryAndAllocateNoKernelBoxes()
        => AssertNumericKernel<double>();

    private void AssertNumericKernel<T>() where T : struct, INumber<T>
    {
        NumericInput<T>[] input = [new(T.One), new(null), new(T.Zero), new(T.Zero)];
        using var query = InstanceCreator.CompileForExecution("select Sum(Value) as Total from #schema.first()",
            Guid.NewGuid().ToString(), LibraryExecutionFixture.CreateProvider(input), LoggerResolver, TestCompilationOptions);
        using var table = query.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Total", typeof(T?)));
        Assert.HasCount(1, rows);
        Assert.AreEqual((T?)T.One, rows[0].GetValue<T?>(0));
        AssertNoExecutionBoxes(query);
        var measured = AllocationTestThread.Run(MeasureNumericKernel<T>);
        Assert.AreEqual(100000m, measured.Checksum);
        Assert.AreEqual(0L, measured.Allocated);
    }

    private static (long Allocated, decimal Checksum) MeasureNumericKernel<T>() where T : struct, INumber<T>
    {
        _ = ConsumeNumericKernel<T>(10000);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var checksum = ConsumeNumericKernel<T>(100000);
        return (GC.GetAllocatedBytesForCurrentThread() - before, checksum);
    }

    private static decimal ConsumeNumericKernel<T>(int iterations) where T : struct, INumber<T>
    {
        var state = default(SumAggregateKernel<T>.State);
        var checksum = 0m;
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            SumAggregateKernel<T>.Reset(ref state);
            SumAggregateKernel<T>.Set(ref state, T.One);
            SumAggregateKernel<T>.Set(ref state, null);
            SumAggregateKernel<T>.Set(ref state, T.Zero);
            checksum += decimal.CreateChecked(SumAggregateKernel<T>.Get(in state)!.Value);
        }
        return checksum;
    }

    public sealed record NumericInput<T>(T? Value) where T : struct;
}
