using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionPerformanceQueryTests
{
    [TestMethod]
    public void ByteSum_ShouldExecuteTypedQuery()
        => AssertNumericKernel<byte>();

    [TestMethod]
    public void SByteSum_ShouldExecuteTypedQuery()
        => AssertNumericKernel<sbyte>();

    [TestMethod]
    public void Int16Sum_ShouldExecuteTypedQuery()
        => AssertNumericKernel<short>();

    [TestMethod]
    public void UInt16Sum_ShouldExecuteTypedQuery()
        => AssertNumericKernel<ushort>();

    [TestMethod]
    public void Int32Sum_ShouldExecuteTypedQuery()
        => AssertNumericKernel<int>();

    [TestMethod]
    public void UInt32Sum_ShouldExecuteTypedQuery()
        => AssertNumericKernel<uint>();

    [TestMethod]
    public void Int64Sum_ShouldExecuteTypedQuery()
        => AssertNumericKernel<long>();

    [TestMethod]
    public void UInt64Sum_ShouldExecuteTypedQuery()
        => AssertNumericKernel<ulong>();

    [TestMethod]
    public void DecimalSum_ShouldExecuteTypedQuery()
        => AssertNumericKernel<decimal>();

    [TestMethod]
    public void SingleSum_ShouldExecuteTypedQuery()
        => AssertNumericKernel<float>();

    [TestMethod]
    public void DoubleSum_ShouldExecuteTypedQuery()
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
    }

    public sealed record NumericInput<T>(T? Value) where T : struct;
}
