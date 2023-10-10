using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BitwiseShiftLeftTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenBitwiseShiftLeft_sbyte_ShouldBeEvaluated()
    {
        TestMethodTemplate<sbyte?>("ShiftLeft(1b, 1)", 2);
    }

    [TestMethod]
    public void WhenBitwiseShiftLeft_byte_ShouldBeEvaluated()
    {
        TestMethodTemplate<byte?>("ShiftLeft(1ub, 1)", 2);
    }

    [TestMethod]
    public void WhenBitwiseShiftLeft_short_ShouldBeEvaluated()
    {
        TestMethodTemplate<short?>("ShiftLeft(1s, 1)", 2);
    }

    [TestMethod]
    public void WhenBitwiseShiftLeft_ushort_ShouldBeEvaluated()
    {
        TestMethodTemplate<ushort?>("ShiftLeft(1us, 1)", 2);
    }

    [TestMethod]
    public void WhenBitwiseShiftLeft_int_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("ShiftLeft(1i, 1)", 2);
    }

    [TestMethod]
    public void WhenBitwiseShiftLeft_uint_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("ShiftLeft(1ui, 1)", 2);
    }

    [TestMethod]
    public void WhenBitwiseShiftLeft_long_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("ShiftLeft(1l, 1)", 2);
    }

    [TestMethod]
    public void WhenBitwiseShiftLeft_ulong_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("ShiftLeft(1ul, 1)", 2);
    }
}