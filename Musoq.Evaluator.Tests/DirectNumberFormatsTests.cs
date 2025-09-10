using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class DirectNumberFormatsTests : BasicEntityTestBase
    {
        [TestMethod]
        public void HexadecimalLiteral_BasicTest()
        {
            TestMethodTemplate("0xFF", 255L);
        }

        [TestMethod]
        public void BinaryLiteral_BasicTest()
        {
            TestMethodTemplate("0b1111", 15L);
        }

        [TestMethod]
        public void OctalLiteral_BasicTest()
        {
            TestMethodTemplate("0o77", 63L);
        }

        [TestMethod]
        public void HexBinaryArithmetic_Addition()
        {
            TestMethodTemplate("0xFF + 0b101", 260L);
        }

        [TestMethod]
        public void MultiformatArithmetic_ComplexExpression()
        {
            TestMethodTemplate("0xFF + 0b101 + 0o7 + 42", 309L);
        }

        [TestMethod]
        public void HexSubtraction_BasicTest()
        {
            TestMethodTemplate("0xFF - 0x0A", 245L);
        }

        [TestMethod]
        public void CaseInsensitive_HexLiterals()
        {
            TestMethodTemplate("0XaB + 0xff", 426L);
        }

        [TestMethod]
        public void CaseInsensitive_BinaryLiterals()
        {
            TestMethodTemplate("0B101 + 0b010", 7L);
        }

        [TestMethod]
        public void CaseInsensitive_OctalLiterals()
        {
            TestMethodTemplate("0O77 + 0o01", 64L);
        }

        [TestMethod]
        public void HexMultiplication_Test()
        {
            TestMethodTemplate("0x10 * 0x2", 32L);
        }

        [TestMethod]
        public void BinaryDivision_Test()
        {
            TestMethodTemplate("0b1000 / 0b10", 4L);
        }

        [TestMethod]
        public void OctalModulo_Test()
        {
            TestMethodTemplate("0o17 % 0o5", 0L);
        }
    }
}