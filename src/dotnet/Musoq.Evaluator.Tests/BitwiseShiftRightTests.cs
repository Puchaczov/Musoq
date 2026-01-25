using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BitwiseShiftRightTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenBitwiseShiftRightSByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<sbyte?>("ShiftRight(8b, 1)", 4);
    }

    [TestMethod]
    public void WhenBitwiseShiftRightByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<byte?>("ShiftRight(8ub, 1)", 4);
    }

    [TestMethod]
    public void WhenBitwiseShiftRightShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<short?>("ShiftRight(8s, 1)", 4);
    }

    [TestMethod]
    public void WhenBitwiseShiftRightUShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<ushort?>("ShiftRight(8us, 1)", 4);
    }

    [TestMethod]
    public void WhenBitwiseShiftRightInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("ShiftRight(8i, 1)", 4);
    }

    [TestMethod]
    public void WhenBitwiseShiftRightUInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("ShiftRight(8ui, 1)", 4);
    }

    [TestMethod]
    public void WhenBitwiseShiftRightLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("ShiftRight(8l, 1)", 4);
    }

    [TestMethod]
    public void WhenBitwiseShiftRightULong_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("ShiftRight(8ul, 1)", 4);
    }
}
