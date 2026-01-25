using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BitwiseOperationsAndTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenBitwiseAndBetweenSbyteAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1b, 1ub)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenSbyteAndSbyte_ShouldBeEvaluated()
    {
        TestMethodTemplate<sbyte?>("And(1b, 1b)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenSbyteAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1b, 1s)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenSbyteAndUshort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1b, 1us)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenSbyteAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1b, 1i)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenSbyteAndUint_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("And(1b, 1ui)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenSbyteAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("And(1b, 1l)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenByteAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<byte?>("And(1ub, 1ub)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenByteAndSByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1ub, 1b)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenByteAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1ub, 1s)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenByteAndUShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1ub, 1us)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenByteAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1ub, 1i)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenByteAndUInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("And(1ub, 1ui)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenByteAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("And(1ub, 1l)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenByteAndULong_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("And(1ub, 1ul)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUshortAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1us, 1ub)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUshortAndSbyte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1us, 1b)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUshortAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1us, 1s)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUshortAndUshort_ShouldBeEvaluated()
    {
        TestMethodTemplate<ushort?>("And(1us, 1us)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUshortAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1us, 1i)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUshortAndUint_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("And(1us, 1ui)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUshortAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("And(1us, 1l)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUshortAndUlong_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("And(1us, 1ul)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenShortAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1s, 1ub)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenShortAndSByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1s, 1b)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenShortAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<short?>("And(1s, 1s)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenShortAndUShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1s, 1us)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenShortAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1s, 1i)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenShortAndUInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("And(1s, 1ui)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenShortAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("And(1s, 1l)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenIntAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1i, 1ub)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenIntAndSByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1i, 1b)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenIntAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1i, 1s)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenIntAndUShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1i, 1us)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenIntAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1i, 1i)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenIntAndUInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("And(1i, 1ui)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenIntAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("And(1i, 1l)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenSByteAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("And(1b, 1i)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUIntAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("And(1ui, 1ub)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUIntAndSByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("And(1ui, 1b)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUIntAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("And(1ui, 1s)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUIntAndUShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("And(1ui, 1us)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUIntAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("And(1ui, 1i)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUIntAndUInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("And(1ui, 1ui)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUIntAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("And(1ui, 1l)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUIntAndULong_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("And(1ui, 1ul)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenLongAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("And(1l, 1ub)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenLongAndSByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("And(1l, 1b)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenLongAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("And(1l, 1s)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenLongAndUShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("And(1l, 1us)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenLongAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("And(1l, 1i)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenLongAndUInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("And(1l, 1ui)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenLongAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("And(1l, 1l)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUlongAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("And(1ul, 1ub)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUlongAndUshort_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("And(1ul, 1us)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUlongAndUint_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("And(1ul, 1ui)", 1);
    }

    [TestMethod]
    public void WhenBitwiseAndBetweenUlongAndUlong_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("And(1ul, 1ul)", 1);
    }
}
