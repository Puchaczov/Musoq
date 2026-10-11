using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
    [TestMethod] public void ByteWindowSum_ShouldThrowCheckedOverflow() => AssertWindowOverflow(byte.MaxValue);
    [TestMethod] public void SByteWindowSum_ShouldThrowCheckedOverflow() => AssertWindowOverflow(sbyte.MaxValue);
    [TestMethod] public void Int16WindowSum_ShouldThrowCheckedOverflow() => AssertWindowOverflow(short.MaxValue);
    [TestMethod] public void UInt16WindowSum_ShouldThrowCheckedOverflow() => AssertWindowOverflow(ushort.MaxValue);
    [TestMethod] public void Int32WindowSum_ShouldThrowCheckedOverflow() => AssertWindowOverflow(int.MaxValue);
    [TestMethod] public void UInt32WindowSum_ShouldThrowCheckedOverflow() => AssertWindowOverflow(uint.MaxValue);
    [TestMethod] public void Int64WindowSum_ShouldThrowCheckedOverflow() => AssertWindowOverflow(long.MaxValue);
    [TestMethod] public void UInt64WindowSum_ShouldThrowCheckedOverflow() => AssertWindowOverflow(ulong.MaxValue);
    [TestMethod] public void DecimalWindowSum_ShouldThrowCheckedOverflow() => AssertWindowOverflow(decimal.MaxValue);

    [TestMethod]
    public void FloatingFrames_WhenValuesAreOrderSensitive_ShouldAccumulateEachFrameInOrder()
    {
        const string query = """
            select Position,
                Sum(Value) over (order by Position rows between unbounded preceding and current row) as Running,
                Sum(Value) over (order by Position rows between 1 preceding and current row) as Bounded
            from #schema.first() order by Position
            """;
        NumericRow<double>[] input = [new(1, 1e16), new(2, 1d), new(3, -1e16), new(4, 1d)];
        using var compiled = CreateAndRunVirtualMachine(query, input);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)),
            ("Running", typeof(double?)), ("Bounded", typeof(double?)));
        Assert.HasCount(4, rows);
        double[] running = [1e16, 1e16, 0d, 1d];
        double[] bounded = [1e16, 1e16, -1e16, -1e16];
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(running[index], rows[index].GetValue<double?>(1));
            Assert.AreEqual(bounded[index], rows[index].GetValue<double?>(2));
        }
    }

    [TestMethod]
    public void BoundedFrames_WhenEarlierValuesOverflowOutsideTheFrame_ShouldIgnoreThoseValues()
    {
        const string query = """
            select Position, Sum(Value) over (order by Position rows between current row and current row) as Total
            from #schema.first() order by Position
            """;
        NumericRow<long>[] input = [new(1, long.MaxValue), new(2, 1), new(3, -long.MaxValue)];
        using var compiled = CreateAndRunVirtualMachine(query, input);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Total", typeof(long?)));
        Assert.HasCount(3, rows);
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(input[index].Position, rows[index].GetValue<int>(0));
            Assert.AreEqual(input[index].Value, rows[index].GetValue<long?>(1));
        }
    }

    private void AssertWindowOverflow<T>(T maximum) where T : struct, INumber<T>
    {
        const string query = """
            select Sum(Value) over (order by Position rows between unbounded preceding and current row) as Total
            from #schema.first()
            """;
        NumericRow<T>[] input = [new(1, maximum), new(2, T.One)];
        using var compiled = CreateAndRunVirtualMachine(query, input);

        var error = Assert.Throws<QueryExecutionException>(() => compiled.Run(TestContext.CancellationToken).ToArray());

        Assert.IsInstanceOfType<OverflowException>(error.InnerException);
    }
}
