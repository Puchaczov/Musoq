using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ArithmeticTests : BasicEntityTestBase
{
    [TestMethod]
    public void SimpleAdditionSubtractionTest()
    {
        TestMethodTemplate("1 + 1 + 2 - 2", 2);
    }
        
    [TestMethod]
    public void SimpleAdditionSubtraction2Test()
    {
        TestMethodTemplate("1 + 1 + 2 + (-2)", 2);
    }
        
    [TestMethod]
    public void SimpleAdditionSubtraction3Test()
    {
        TestMethodTemplate("1 + 1 + 2 + -2", 2);
    }
        
    [TestMethod]
    public void MultiDividingTest()
    {
        TestMethodTemplate("256 / 8 / 8", 4);
    }

    [TestMethod]
    public void DivisionWithAdditionTest()
    {
        TestMethodTemplate("256 + 256 / 2", 384);
    }

    [TestMethod]
    public void DivisionWithAdditionHigherPriorityTest()
    {
        TestMethodTemplate("(256 + 256) / 2", 256);
    }

    [TestMethod]
    public void DivisionWithMultiplicationTest()
    {
        TestMethodTemplate("2 * 3 / 2", 3);
    }

    [TestMethod]
    public void DivisionWithSubtraction1Test()
    {
        TestMethodTemplate("128 / 64 - 2", 0);
    }

    [TestMethod]
    public void DivisionWithSubtraction2Test()
    {
        TestMethodTemplate("128 - 64 / 2", 96);
    }

    [TestMethod]
    public void MultiSubtractionTest()
    {
        TestMethodTemplate("256 - 128 - 128", 0);
    }

    [TestMethod]
    public void SubtractionWithAdditionTest()
    {
        TestMethodTemplate("256 + 128 - 128", 256);
    }

    [TestMethod]
    public void SubtractionWithUnaryMinusTest()
    {
        TestMethodTemplate("1 - -1", 2);
    }

    [TestMethod]
    public void SubtractionWithUnaryMinusExpressionTest()
    {
        TestMethodTemplate("1 - -(1 + 2)", 4);
    }

    [TestMethod]
    public void SubtractionWithUnaryMinusAndAdditionTest()
    {
        TestMethodTemplate("1 - -1 + 2", 4);
    }

    [TestMethod]
    public void SubtractionWithUnaryMinusAndSubtractionTest()
    {
        TestMethodTemplate("1 - (-1) - 2", 0);
    }

    [TestMethod]
    public void AdditionWithUnaryMinusAndAdditionTest()
    {
        TestMethodTemplate("1 + -1 + 2", 2);
    }

    [TestMethod]
    public void ModuloExpressionWithMultiplicationTest()
    {
        TestMethodTemplate("8 % 3 * 2", 4);
    }

    [TestMethod]
    public void ModuloExpressionWithSubtractionTest()
    {
        TestMethodTemplate("8 % 3 - 2", 0);
    }

    [TestMethod]
    public void ModuloExpressionWithAdditionTest()
    {
        TestMethodTemplate("8 % 3 + 2", 4);
    }

    [TestMethod]
    public void MultiModuloExpressionTest()
    {
        TestMethodTemplate("5 % 4 % 6", 1);
    }

    [TestMethod]
    public void ComplexArithmeticExpression1Test()
    {
        TestMethodTemplate("1 + 2 * 3 * ( 7 * 8 ) - ( 45 - 10 )", 302);
    }

    [TestMethod]
    public void CaseWhenArithmeticExpressionTest()
    {
        TestMethodTemplate("1 + (case when 2 > 1 then 1 else 0 end) - 1", 1);
    }

    [TestMethod]
    public void ComplexNestedArithmeticExpressionTest()
    {
        // Original 6-level deeply nested expression with 30+ arithmetic operations
        // Fixed by caching ReturnType in BinaryNode constructor (was O(2^n), now O(n))
        TestMethodTemplate("(((((1 + (6 * 2)) + 4 + 4 + 4 + 2 + 8 + 1 + 4 + 1 + 1 + 1 + 1 + 1 + 1 + 32 + 1 + 4 + 4 + 4 + 1 + 4 + 4 + 1 + (6 * 4) + 1 + 1 + 1 + 1 + 32 + 1) + 4) + 1 + 1) + 4 + 4) + 4 + 4 + 4", 188);
    }
    
    [TestMethod]
    public void VeryLongArithmeticChain_ShouldEvaluateCorrectly()
    {
        // Test with 50 additions - sum of 1 to 50 = 1275
        var expected = System.Linq.Enumerable.Range(1, 50).Sum();
        var expr = string.Join(" + ", System.Linq.Enumerable.Range(1, 50).Select(i => i.ToString()));
        TestMethodTemplate(expr, expected);
    }
    
    [TestMethod]
    public void DeeplyNestedParentheses_ShouldEvaluateCorrectly()
    {
        // Test with 10 levels of parentheses nesting
        TestMethodTemplate("((((((((((1 + 2))))))))))", 3);
    }
    
    [TestMethod]
    public void ComplexMixedOperators_ShouldRespectPrecedence()
    {
        // 1 + 2*3 - 4/2 + 5*6 - 7 + 8/4 = 1 + 6 - 2 + 30 - 7 + 2 = 30
        TestMethodTemplate("1 + 2 * 3 - 4 / 2 + 5 * 6 - 7 + 8 / 4", 30);
    }
    
    [TestMethod]
    public void NestedSubExpressions_WithParentheses()
    {
        // ((1 + 2) * (3 - 4)) / ((5 + 6) - (7 * 8)) = (3 * -1) / (11 - 56) = -3 / -45 = 0
        TestMethodTemplate("((1 + 2) * (3 - 4)) / ((5 + 6) - (7 * 8))", 0);
    }
    
    [TestMethod]
    public void MultipleNestedLevels_ComplexExpression()
    {
        // (1 + (2 * (3 - (4 / (5 + 6))))) = 1 + (2 * (3 - (4/11))) = 1 + (2 * 3) = 7 (integer division)
        TestMethodTemplate("(1 + (2 * (3 - (4 / (5 + 6)))))", 7);
    }
    
    [TestMethod]
    public void LongChainWithMixedOperators()
    {
        // Test a long chain with various operators
        // 10 + 5 - 3 * 2 + 8 / 4 - 1 = 10 + 5 - 6 + 2 - 1 = 10
        TestMethodTemplate("10 + 5 - 3 * 2 + 8 / 4 - 1", 10);
    }
    
    [TestMethod]
    public void NestedParenthesesWithOperatorPrecedence()
    {
        // (2 + 3) * (4 + 5) - (6 * 7) + (8 / 2) = 5 * 9 - 42 + 4 = 45 - 42 + 4 = 7
        TestMethodTemplate("(2 + 3) * (4 + 5) - (6 * 7) + (8 / 2)", 7);
    }
    
    [TestMethod]
    public void ExtremeLongExpression_ShouldEvaluateCorrectly()
    {
        // Test with 100 additions - sum of 1 to 100 = 5050
        var expected = System.Linq.Enumerable.Range(1, 100).Sum();
        var expr = string.Join(" + ", System.Linq.Enumerable.Range(1, 100).Select(i => i.ToString()));
        TestMethodTemplate(expr, expected);
    }
    
    [TestMethod]
    public void MultiplicationAndDivisionChain()
    {
        // 100 * 2 / 4 * 3 / 5 = 200 / 4 * 3 / 5 = 50 * 3 / 5 = 150 / 5 = 30
        TestMethodTemplate("100 * 2 / 4 * 3 / 5", 30);
    }
    
    [TestMethod]
    public void AdditionAndSubtractionChain()
    {
        // 100 - 20 + 15 - 10 + 25 - 5 = 105
        TestMethodTemplate("100 - 20 + 15 - 10 + 25 - 5", 105);
    }
    
    [TestMethod]
    public void ComplexNestedWithAllOperators()
    {
        // ((10 + 5) * 2 - (8 / 4)) + ((6 - 2) * (3 + 1)) = (15 * 2 - 2) + (4 * 4) = 28 + 16 = 44
        TestMethodTemplate("((10 + 5) * 2 - (8 / 4)) + ((6 - 2) * (3 + 1))", 44);
    }
    
    [TestMethod]
    public void StressTest_CombinedComplexity()
    {
        // Combination of long chains and deep nesting
        // ((1+2+3+4+5) * 2) + ((6+7+8+9+10) * 2) = (15 * 2) + (40 * 2) = 30 + 80 = 110
        TestMethodTemplate("((1+2+3+4+5) * 2) + ((6+7+8+9+10) * 2)", 110);
    }
}