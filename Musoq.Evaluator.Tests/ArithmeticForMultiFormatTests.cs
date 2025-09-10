using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ArithmeticForMultiFormatTests : BasicEntityTestBase
{
    [TestMethod]
    public void HexAdditionTest()
    {
        TestMethodTemplate("FromHex('FF') + FromHex('01')", 256L);
    }

    [TestMethod]
    public void HexSubtractionTest()
    {
        TestMethodTemplate("FromHex('FF') - FromHex('0A')", 245L);
    }

    [TestMethod]
    public void HexMultiplicationTest()
    {
        TestMethodTemplate("FromHex('A') * FromHex('2')", 20L);
    }

    [TestMethod]
    public void HexDivisionTest()
    {
        TestMethodTemplate("FromHex('20') / FromHex('4')", 8L);
    }

    [TestMethod]
    public void BinaryAdditionTest()
    {
        TestMethodTemplate("FromBin('1010') + FromBin('0101')", 15L);
    }

    [TestMethod]
    public void BinarySubtractionTest()
    {
        TestMethodTemplate("FromBin('1111') - FromBin('0101')", 10L);
    }

    [TestMethod]
    public void BinaryMultiplicationTest()
    {
        TestMethodTemplate("FromBin('101') * FromBin('11')", 15L);
    }

    [TestMethod]
    public void BinaryDivisionTest()
    {
        TestMethodTemplate("FromBin('1000') / FromBin('10')", 4L);
    }

    [TestMethod]
    public void OctalAdditionTest()
    {
        TestMethodTemplate("FromOct('77') + FromOct('1')", 64L);
    }

    [TestMethod]
    public void OctalSubtractionTest()
    {
        TestMethodTemplate("FromOct('100') - FromOct('10')", 56L);
    }

    [TestMethod]
    public void OctalMultiplicationTest()
    {
        TestMethodTemplate("FromOct('7') * FromOct('2')", 14L);
    }

    [TestMethod]
    public void OctalDivisionTest()
    {
        TestMethodTemplate("FromOct('20') / FromOct('4')", 4L);
    }

    [TestMethod]
    public void HexWithIntArithmeticTest()
    {
        TestMethodTemplate("FromHex('FF') + 1", 256L);
        TestMethodTemplate("FromHex('10') - 6", 10L);
        TestMethodTemplate("10 + FromHex('A')", 20L);
        TestMethodTemplate("20 - FromHex('5')", 15L);
    }

    [TestMethod]
    public void BinaryWithIntArithmeticTest()
    {
        TestMethodTemplate("FromBin('1010') + 5", 15L);
        TestMethodTemplate("FromBin('1111') - 5", 10L);
        TestMethodTemplate("10 + FromBin('101')", 15L);
        TestMethodTemplate("20 - FromBin('110')", 14L);
    }

    [TestMethod]
    public void OctalWithIntArithmeticTest()
    {
        TestMethodTemplate("FromOct('10') + 2", 10L);
        TestMethodTemplate("FromOct('20') - 8", 8L);
        TestMethodTemplate("10 + FromOct('7')", 17L);
        TestMethodTemplate("20 - FromOct('5')", 15L);
    }

    [TestMethod]
    public void HexBinaryArithmeticTest()
    {
        TestMethodTemplate("FromHex('F') + FromBin('1')", 16L);
        TestMethodTemplate("FromHex('10') - FromBin('1010')", 6L);
        TestMethodTemplate("FromBin('1111') + FromHex('1')", 16L);
        TestMethodTemplate("FromBin('10000') - FromHex('8')", 8L);
    }

    [TestMethod]
    public void HexOctalArithmeticTest()
    {
        TestMethodTemplate("FromHex('F') + FromOct('1')", 16L);
        TestMethodTemplate("FromHex('20') - FromOct('10')", 24L);
        TestMethodTemplate("FromOct('77') + FromHex('1')", 64L);
        TestMethodTemplate("FromOct('100') - FromHex('20')", 32L);
    }

    [TestMethod]
    public void BinaryOctalArithmeticTest()
    {
        TestMethodTemplate("FromBin('1111') + FromOct('1')", 16L);
        TestMethodTemplate("FromBin('10000') - FromOct('10')", 8L);
        TestMethodTemplate("FromOct('7') + FromBin('1')", 8L);
        TestMethodTemplate("FromOct('10') - FromBin('110')", 2L);
    }

    [TestMethod]
    public void ComplexMultiFormatArithmeticTest()
    {
        TestMethodTemplate("FromHex('A') + FromBin('101') + FromOct('7') + 2", 24L);
        TestMethodTemplate("FromHex('FF') - FromBin('1111') - FromOct('10') - 5", 227L);
        TestMethodTemplate("(FromHex('10') + FromBin('1010')) * FromOct('2')", 52L);
        TestMethodTemplate("FromHex('64') / (FromBin('10') + FromOct('2'))", 25L);
    }

    [TestMethod]
    public void HexWithPrefixArithmeticTest()
    {
        TestMethodTemplate("FromHex('0xFF') + FromHex('0x01')", 256L);
        TestMethodTemplate("FromHex('0xFF') - FromHex('0x0A')", 245L);
    }

    [TestMethod]
    public void BinaryWithPrefixArithmeticTest()
    {
        TestMethodTemplate("FromBin('0b1010') + FromBin('0b0101')", 15L);
        TestMethodTemplate("FromBin('0b1111') - FromBin('0b0101')", 10L);
    }

    [TestMethod]
    public void OctalWithPrefixArithmeticTest()
    {
        TestMethodTemplate("FromOct('0o77') + FromOct('0o1')", 64L);
        TestMethodTemplate("FromOct('0o100') - FromOct('0o10')", 56L);
    }

    [TestMethod]
    public void ModuloOperationsWithFormatsTest()
    {
        TestMethodTemplate("FromHex('A') % 3", 1L);
        TestMethodTemplate("FromBin('1010') % 3", 1L);
        TestMethodTemplate("FromOct('12') % 5", 0L);
    }

    [TestMethod]
    public void NegativeNumberFormatsTest()
    {
        TestMethodTemplate("0 - FromHex('A')", -10L);
        TestMethodTemplate("0 - FromBin('101')", -5L);
        TestMethodTemplate("0 - FromOct('7')", -7L);
    }

    [TestMethod]
    public void CaseInsensitiveHexTest()
    {
        TestMethodTemplate("FromHex('ff') + FromHex('FF')", 510L);
        TestMethodTemplate("FromHex('0xff') + FromHex('0XFF')", 510L);
    }

    [TestMethod]
    public void CaseInsensitiveBinaryTest()
    {
        TestMethodTemplate("FromBin('0b101') + FromBin('0B101')", 10L);
    }

    [TestMethod]
    public void CaseInsensitiveOctalTest()
    {
        TestMethodTemplate("FromOct('0o7') + FromOct('0O7')", 14L);
    }
}