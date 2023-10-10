using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BitwiseOperationsXorTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenBitwiseXorBetweenSbyteAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1b, 1ub)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenSbyteAndSbyte_ShouldBeEvaluated()
    {
        TestMethodTemplate<sbyte?>("Xor(1b, 1b)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenSbyteAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1b, 1s)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenSbyteAndUshort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1b, 1us)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenSbyteAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1b, 1i)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenSbyteAndUint_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Xor(1b, 1ui)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenSbyteAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Xor(1b, 1l)", 0);
    }
    
    [TestMethod]
    public void WhenBitwiseXorBetweenByteAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<byte?>("Xor(1ub, 1ub)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenByteAndSByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1ub, 1b)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenByteAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1ub, 1s)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenByteAndUShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1ub, 1us)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenByteAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1ub, 1i)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenByteAndUInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Xor(1ub, 1ui)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenByteAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Xor(1ub, 1l)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenByteAndULong_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("Xor(1ub, 1ul)", 0);
    }
    
    [TestMethod]
    public void WhenBitwiseXorBetweenUshortAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1us, 1ub)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenUshortAndSbyte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1us, 1b)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenUshortAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1us, 1s)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenUshortAndUshort_ShouldBeEvaluated()
    {
        TestMethodTemplate<ushort?>("Xor(1us, 1us)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenUshortAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1us, 1i)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenUshortAndUint_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Xor(1us, 1ui)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenUshortAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Xor(1us, 1l)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenUshortAndUlong_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("Xor(1us, 1ul)", 0);
    }
    
    [TestMethod]
    public void WhenBitwiseXorBetweenShortAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1s, 1ub)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenShortAndSByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1s, 1b)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenShortAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<short?>("Xor(1s, 1s)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenShortAndUShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1s, 1us)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenShortAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1s, 1i)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenShortAndUInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Xor(1s, 1ui)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenShortAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Xor(1s, 1l)", 0);
    }
    
    [TestMethod]
    public void WhenBitwiseXorBetweenIntAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1i, 1ub)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenIntAndSByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1i, 1b)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenIntAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1i, 1s)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenIntAndUShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1i, 1us)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenIntAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1i, 1i)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenIntAndUInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Xor(1i, 1ui)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenIntAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Xor(1i, 1l)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenSByteAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<int?>("Xor(1b, 1i)", 0);
    }
    
    [TestMethod]
    public void WhenBitwiseXorBetweenUIntAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Xor(1ui, 1ub)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenUIntAndSByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Xor(1ui, 1b)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenUIntAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Xor(1ui, 1s)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenUIntAndUShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Xor(1ui, 1us)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenUIntAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Xor(1ui, 1i)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenUIntAndUInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<uint?>("Xor(1ui, 1ui)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenUIntAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("Xor(1ui, 1l)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenUIntAndULong_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("Xor(1ui, 1ul)", 0);
    }
    
    [TestMethod]
    public void WhenBitwiseXorBetweenLongAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Xor(1l, 1ub)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenLongAndSByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Xor(1l, 1b)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenLongAndShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Xor(1l, 1s)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenLongAndUShort_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Xor(1l, 1us)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenLongAndInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Xor(1l, 1i)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenLongAndUInt_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Xor(1l, 1ui)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenLongAndLong_ShouldBeEvaluated()
    {
        TestMethodTemplate<long?>("Xor(1l, 1l)", 0);
    }
    
    [TestMethod]
    public void WhenBitwiseXorBetweenUlongAndByte_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("Xor(1ul, 1ub)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenUlongAndUshort_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("Xor(1ul, 1us)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenUlongAndUint_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("Xor(1ul, 1ui)", 0);
    }

    [TestMethod]
    public void WhenBitwiseXorBetweenUlongAndUlong_ShouldBeEvaluated()
    {
        TestMethodTemplate<ulong?>("Xor(1ul, 1ul)", 0);
    }
}