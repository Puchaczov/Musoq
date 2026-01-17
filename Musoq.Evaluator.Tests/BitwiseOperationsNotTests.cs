using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BitwiseOperationsNotTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenBitwiseNotOnSByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<sbyte?>("Not(1b)", -2);
    }

    [TestMethod]
    public void WhenBitwiseNotOnByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<byte?>("Not(1ub)", 254);
    }

    [TestMethod]
    public void WhenBitwiseNotOnShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<short?>("Not(1s)", -2);
    }

    [TestMethod]
    public void WhenBitwiseNotOnUShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<ushort?>("Not(1us)", 65534);
    }

    [TestMethod]
    public void WhenBitwiseNotOnInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Not(1i)", -2);
    }

    [TestMethod]
    public void WhenBitwiseNotOnUInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Not(1ui)", 4294967294);
    }

    [TestMethod]
    public void WhenBitwiseNotOnLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Not(1l)", -2);
    }

    [TestMethod]
    public void WhenBitwiseNotOnULong_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("Not(1ul)", 18446744073709551614);
    }
}