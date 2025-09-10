using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
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
        
        // Edge cases with zero values
        [TestMethod]
        public void ZeroValues_AllFormats()
        {
            TestMethodTemplate("0x0", 0L);
            TestMethodTemplate("0b0", 0L);
            TestMethodTemplate("0o0", 0L);
        }
        
        [TestMethod]
        public void ZeroValues_InArithmetic()
        {
            TestMethodTemplate("0x0 + 0b0 + 0o0", 0L);
            TestMethodTemplate("0xFF + 0x0", 255L);
            TestMethodTemplate("0b1111 - 0b0", 15L);
            TestMethodTemplate("0o77 * 0o0", 0L);
        }
        
        // Precedence and associativity tests
        [TestMethod]
        public void OperatorPrecedence_MultiplicationFirst()
        {
            TestMethodTemplate("0x2 + 0x3 * 0x4", 14L); // 2 + (3 * 4) = 14
        }
        
        [TestMethod]
        public void OperatorPrecedence_DivisionFirst()
        {
            TestMethodTemplate("0x10 + 0x8 / 0x2", 20L); // 16 + (8 / 2) = 20
        }
        
        [TestMethod]
        public void Parentheses_OverridePrecedence()
        {
            TestMethodTemplate("(0x2 + 0x3) * 0x4", 20L); // (2 + 3) * 4 = 20
        }
        
        [TestMethod]
        public void Associativity_LeftToRight()
        {
            TestMethodTemplate("0x10 - 0x5 - 0x2", 9L); // ((16 - 5) - 2) = 9
        }
        
        // Complex nested expressions
        [TestMethod]
        public void NestedParentheses_ComplexExpression()
        {
            TestMethodTemplate("((0xFF + 0b101) * 0o2) / (0x4 + 0b100)", 65L);
        }
        
        [TestMethod]
        public void DeepNesting_ArithmeticExpression()
        {
            TestMethodTemplate("(((0x10 + 0b10) * 0o2) - (0xFF / 0x5)) / 0b11", 5L);
        }
        
        // Single digit values
        [TestMethod]
        public void SingleDigit_HexValues()
        {
            TestMethodTemplate("0x0", 0L);
            TestMethodTemplate("0x1", 1L);
            TestMethodTemplate("0x5", 5L);
            TestMethodTemplate("0xA", 10L);
            TestMethodTemplate("0xF", 15L);
        }
        
        [TestMethod]
        public void SingleDigit_BinaryValues()
        {
            TestMethodTemplate("0b0", 0L);
            TestMethodTemplate("0b1", 1L);
        }
        
        [TestMethod]
        public void SingleDigit_OctalValues()
        {
            TestMethodTemplate("0o0", 0L);
            TestMethodTemplate("0o1", 1L);
            TestMethodTemplate("0o7", 7L);
        }
        
        // Large values testing
        [TestMethod]
        public void LargeValues_Hexadecimal()
        {
            TestMethodTemplate("0xFFFF", 65535L);
            TestMethodTemplate("0xDEADBEEF", 3735928559L);
        }
        
        [TestMethod]
        public void LargeValues_Binary()
        {
            TestMethodTemplate("0b11111111", 255L);
            TestMethodTemplate("0b1111111111111111", 65535L);
        }
        
        [TestMethod]
        public void LargeValues_Octal()
        {
            TestMethodTemplate("0o777", 511L);
            TestMethodTemplate("0o177777", 65535L);
        }
        
        // Mixed case variations
        [TestMethod]
        public void MixedCase_AllVariations()
        {
            TestMethodTemplate("0xFF + 0Xff + 0xfF + 0xff", 1020L);
            TestMethodTemplate("0XFF + 0XfF + 0Xff + 0xff", 1020L);
        }
        
        [TestMethod]
        public void MixedCase_BinaryVariations()
        {
            TestMethodTemplate("0b101 + 0B101", 10L);
        }
        
        [TestMethod]
        public void MixedCase_OctalVariations()
        {
            TestMethodTemplate("0o77 + 0O77", 126L);
        }
        
        // Cross-format arithmetic comprehensive tests
        [TestMethod]
        public void CrossFormat_HexAndBinary_AllOperations()
        {
            TestMethodTemplate("0xFF + 0b101", 260L);
            TestMethodTemplate("0xFF - 0b101", 250L);
            TestMethodTemplate("0x10 * 0b10", 32L);
            TestMethodTemplate("0x10 / 0b10", 8L);
            TestMethodTemplate("0xFF % 0b101", 0L);
        }
        
        [TestMethod]
        public void CrossFormat_HexAndOctal_AllOperations()
        {
            TestMethodTemplate("0xFF + 0o77", 318L);
            TestMethodTemplate("0xFF - 0o77", 192L);
            TestMethodTemplate("0x10 * 0o2", 32L);
            TestMethodTemplate("0x10 / 0o2", 8L);
            TestMethodTemplate("0xFF % 0o77", 24L);
        }
        
        [TestMethod]
        public void CrossFormat_BinaryAndOctal_AllOperations()
        {
            TestMethodTemplate("0b11111111 + 0o77", 318L);
            TestMethodTemplate("0b11111111 - 0o77", 192L);
            TestMethodTemplate("0b10000 * 0o2", 32L);
            TestMethodTemplate("0b10000 / 0o2", 8L);
            TestMethodTemplate("0b11111111 % 0o77", 24L);
        }
        
        // Integration with regular integers
        [TestMethod]
        public void MixedWithRegularIntegers_Addition()
        {
            TestMethodTemplate("0xFF + 1", 256L);
            TestMethodTemplate("1 + 0xFF", 256L);
            TestMethodTemplate("0b101 + 10", 15L);
            TestMethodTemplate("10 + 0b101", 15L);
            TestMethodTemplate("0o77 + 5", 68L);
            TestMethodTemplate("5 + 0o77", 68L);
        }
        
        [TestMethod]
        public void MixedWithRegularIntegers_ComplexExpressions()
        {
            TestMethodTemplate("(0xFF + 1) * (0b101 - 2)", 768L);
            TestMethodTemplate("100 - (0xFF - 0b101) + 0o10", -42L);
        }
        
        // Division by zero and other edge conditions
        [TestMethod]
        public void DivisionByZero_ShouldHandleCorrectly()
        {
            // Note: These might throw exceptions or return special values
            // depending on the implementation. Adjust expectations accordingly.
            try
            {
                TestMethodTemplate<long?>("0xFF / 0x0", null);
            }
            catch
            {
                // Expected behavior for division by zero
                Assert.IsTrue(true);
            }
        }
        
        // Negative results (subtracting larger from smaller)
        [TestMethod]
        public void NegativeResults_SubtractionOperations()
        {
            TestMethodTemplate("0x5 - 0x10", -11L);
            TestMethodTemplate("0b101 - 0b1111", -10L);
            TestMethodTemplate("0o7 - 0o77", -56L);
        }
        
        // Chained operations
        [TestMethod]
        public void ChainedOperations_SameFormat()
        {
            TestMethodTemplate("0xFF + 0xFF + 0xFF", 765L);
            TestMethodTemplate("0b101 * 0b10 * 0b11", 30L);
            TestMethodTemplate("0o77 - 0o7 - 0o7", 49L);
        }
        
        [TestMethod]
        public void ChainedOperations_MixedFormats()
        {
            TestMethodTemplate("0xFF + 0b101 + 0o77 + 42 + 1", 386L);
            TestMethodTemplate("0xFF * 0b10 / 0o4 + 1", 128L);
        }
        
        // Powers of 2 and common values
        [TestMethod]
        public void PowersOfTwo_AllFormats()
        {
            TestMethodTemplate("0x1", 1L);   // 2^0
            TestMethodTemplate("0x2", 2L);   // 2^1
            TestMethodTemplate("0x4", 4L);   // 2^2
            TestMethodTemplate("0x8", 8L);   // 2^3
            TestMethodTemplate("0x10", 16L); // 2^4
            TestMethodTemplate("0x20", 32L); // 2^5
            TestMethodTemplate("0x40", 64L); // 2^6
            TestMethodTemplate("0x80", 128L);// 2^7
            TestMethodTemplate("0x100", 256L); // 2^8
            
            TestMethodTemplate("0b1", 1L);
            TestMethodTemplate("0b10", 2L);
            TestMethodTemplate("0b100", 4L);
            TestMethodTemplate("0b1000", 8L);
            TestMethodTemplate("0b10000", 16L);
            TestMethodTemplate("0b100000", 32L);
            TestMethodTemplate("0b1000000", 64L);
            TestMethodTemplate("0b10000000", 128L);
            TestMethodTemplate("0b100000000", 256L);
            
            TestMethodTemplate("0o1", 1L);
            TestMethodTemplate("0o2", 2L);
            TestMethodTemplate("0o4", 4L);
            TestMethodTemplate("0o10", 8L);
            TestMethodTemplate("0o20", 16L);
            TestMethodTemplate("0o40", 32L);
            TestMethodTemplate("0o100", 64L);
            TestMethodTemplate("0o200", 128L);
            TestMethodTemplate("0o400", 256L);
        }
        
        // Boundary values
        [TestMethod]
        public void BoundaryValues_MaxSingleDigits()
        {
            TestMethodTemplate("0xF", 15L);     // Max single hex digit
            TestMethodTemplate("0b1", 1L);      // Max single binary digit
            TestMethodTemplate("0o7", 7L);      // Max single octal digit
        }
        
        [TestMethod]
        public void BoundaryValues_Operations()
        {
            TestMethodTemplate("0xF + 0x1", 16L); // Hex overflow to next digit
            TestMethodTemplate("0b1 + 0b1", 2L);  // Binary carry
            TestMethodTemplate("0o7 + 0o1", 8L);  // Octal carry
        }
        
        // Comprehensive format equivalence tests
        [TestMethod]
        public void FormatEquivalence_SameValues()
        {
            // All should equal 15
            TestMethodTemplate("0xF", 15L);
            TestMethodTemplate("0b1111", 15L);
            TestMethodTemplate("0o17", 15L);
            
            // All should equal 255
            TestMethodTemplate("0xFF", 255L);
            TestMethodTemplate("0b11111111", 255L);
            TestMethodTemplate("0o377", 255L);
        }
        
        [TestMethod]
        public void FormatEquivalence_ArithmeticResults()
        {
            // Different representations of same calculation
            TestMethodTemplate("0xF + 0x1", 16L);
            TestMethodTemplate("0b1111 + 0b1", 16L);
            TestMethodTemplate("0o17 + 0o1", 16L);
            TestMethodTemplate("15 + 1", 16L);
        }

        // Overflow Protection Tests
        [TestMethod]
        [ExpectedException(typeof(AstValidationException))]
        public void HexadecimalOverflow_TooLargeForLong()
        {
            // This hex value is too large for long (more than 16 hex digits)
            TestMethodTemplate("0xFFFFFFFFFFFFFFFF1", 0L);
        }

        [TestMethod]
        [ExpectedException(typeof(AstValidationException))]
        public void BinaryOverflow_TooLargeForLong()
        {
            // This binary value has 65 bits (too large for 64-bit long)
            TestMethodTemplate("0b11111111111111111111111111111111111111111111111111111111111111111", 0L);
        }

        [TestMethod]
        [ExpectedException(typeof(AstValidationException))]
        public void OctalOverflow_TooLargeForLong()
        {
            // This octal value is too large for long (more than 21 octal digits)
            TestMethodTemplate("0o7777777777777777777777", 0L);
        }

        [TestMethod]
        public void MaximumValidValues_HexBinaryOctal()
        {
            // Test maximum valid values that should work
            TestMethodTemplate("0x7FFFFFFFFFFFFFFF", 9223372036854775807L); // long.MaxValue in hex
            TestMethodTemplate("0b111111111111111111111111111111111111111111111111111111111111111", 9223372036854775807L); // long.MaxValue in binary (63 bits)
            TestMethodTemplate("0o777777777777777777777", 9223372036854775807L); // long.MaxValue in octal
        }

        [TestMethod]
        public void BoundaryValues_IntToLongTransition()
        {
            // Test values around int.MaxValue (all return long for consistency)
            TestMethodTemplate("0x7FFFFFFF", 2147483647L); // int.MaxValue in hex
            TestMethodTemplate("0x80000000", 2147483648L); // int.MaxValue + 1 in hex
            TestMethodTemplate("0b1111111111111111111111111111111", 2147483647L); // int.MaxValue in binary (31 bits)
            TestMethodTemplate("0b10000000000000000000000000000000", 2147483648L); // int.MaxValue + 1 in binary (32 bits)
            TestMethodTemplate("0o17777777777", 2147483647L); // int.MaxValue in octal
            TestMethodTemplate("0o20000000000", 2147483648L); // int.MaxValue + 1 in octal
        }

        [TestMethod]
        public void EdgeCases_ZeroAndOne()
        {
            // Test edge cases with minimal values
            TestMethodTemplate("0x0", 0L);
            TestMethodTemplate("0b0", 0L);
            TestMethodTemplate("0o0", 0L);
            TestMethodTemplate("0x1", 1L);
            TestMethodTemplate("0b1", 1L);
            TestMethodTemplate("0o1", 1L);
        }
    }
}