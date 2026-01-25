using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BitwiseOperationsOrTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenBitwiseOrBetweenSbyteAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1b, 1ub)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenSbyteAndSbyte_ShouldBeEvaluated()
    {
        TestMethodTemplate<sbyte?>("Or(1b, 1b)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenSbyteAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1b, 1s)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenSbyteAndUshort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1b, 1us)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenSbyteAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1b, 1i)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenSbyteAndUint_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Or(1b, 1ui)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenSbyteAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Or(1b, 1l)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenByteAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<byte?>("Or(1ub, 1ub)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenByteAndSByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1ub, 1b)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenByteAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1ub, 1s)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenByteAndUShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1ub, 1us)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenByteAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1ub, 1i)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenByteAndUInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Or(1ub, 1ui)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenByteAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Or(1ub, 1l)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenByteAndULong_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("Or(1ub, 1ul)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUshortAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1us, 1ub)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUshortAndSbyte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1us, 1b)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUshortAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1us, 1s)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUshortAndUshort_ShouldBeEvaluated()
    {
        TestMethodTemplate<ushort?>("Or(1us, 1us)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUshortAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1us, 1i)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUshortAndUint_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Or(1us, 1ui)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUshortAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Or(1us, 1l)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUshortAndUlong_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("Or(1us, 1ul)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenShortAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1s, 1ub)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenShortAndSByte_ShouldBeEvaluated()
    {
        short x = 1;
        byte y = 1;
        var result = (ushort)x | y;
        TestMethodTemplate<int?>("Or(1s, 1b)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenShortAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<short?>("Or(1s, 1s)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenShortAndUShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1s, 1us)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenShortAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1s, 1i)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenShortAndUInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Or(1s, 1ui)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenShortAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Or(1s, 1l)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenIntAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1i, 1ub)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenIntAndSByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1i, 1b)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenIntAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1i, 1s)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenIntAndUShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1i, 1us)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenIntAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1i, 1i)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenIntAndUInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Or(1i, 1ui)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenIntAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Or(1i, 1l)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenSByteAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Or(1b, 1i)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUIntAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Or(1ui, 1ub)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUIntAndSByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Or(1ui, 1b)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUIntAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Or(1ui, 1s)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUIntAndUShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Or(1ui, 1us)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUIntAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Or(1ui, 1i)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUIntAndUInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Or(1ui, 1ui)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUIntAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("Or(1ui, 1l)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUIntAndULong_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("Or(1ui, 1ul)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenLongAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Or(1l, 1ub)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenLongAndSByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Or(1l, 1b)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenLongAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Or(1l, 1s)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenLongAndUShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Or(1l, 1us)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenLongAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Or(1l, 1i)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenLongAndUInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Or(1l, 1ui)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenLongAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Or(1l, 1l)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUlongAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("Or(1ul, 1ub)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUlongAndUshort_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("Or(1ul, 1us)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUlongAndUint_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("Or(1ul, 1ui)", 1);
    }

    [TestMethod]
    public void WhenBitwiseOrBetweenUlongAndUlong_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("Or(1ul, 1ul)", 1);
    }
}
