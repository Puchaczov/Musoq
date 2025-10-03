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
        TestMethodTemplate("(((((1 + (6 * 2)) + 4 + 4 + 4 + 2 + 8 + 1 + 4 + 1 + 1 + 1 + 1 + 1 + 1 + 32 + 1 + 4 + 4 + 4 + 1 + 4 + 4 + 1 + (6 * 4) + 1 + 1 + 1 + 1 + 32 + 1) + 4) + 1 + 1) + 4 + 4) + 4 + 4 + 4", 188);
    }
    
    [TestMethod]
    public void VeryLongArithmeticChain_ShouldEvaluateCorrectly()
    {
        var expected = System.Linq.Enumerable.Range(1, 50).Sum();
        var expr = string.Join(" + ", System.Linq.Enumerable.Range(1, 50).Select(i => i.ToString()));
        TestMethodTemplate(expr, expected);
    }
    
    [TestMethod]
    public void DeeplyNestedParentheses_ShouldEvaluateCorrectly()
    {
        TestMethodTemplate("((((((((((1 + 2))))))))))", 3);
    }
    
    [TestMethod]
    public void ComplexMixedOperators_ShouldRespectPrecedence()
    {
        TestMethodTemplate("1 + 2 * 3 - 4 / 2 + 5 * 6 - 7 + 8 / 4", 30);
    }
    
    [TestMethod]
    public void NestedSubExpressions_WithParentheses()
    {
        TestMethodTemplate("((1 + 2) * (3 - 4)) / ((5 + 6) - (7 * 8))", 0);
    }
    
    [TestMethod]
    public void MultipleNestedLevels_ComplexExpression()
    {
        TestMethodTemplate("(1 + (2 * (3 - (4 / (5 + 6)))))", 7);
    }
    
    [TestMethod]
    public void LongChainWithMixedOperators()
    {
        TestMethodTemplate("10 + 5 - 3 * 2 + 8 / 4 - 1", 10);
    }
    
    [TestMethod]
    public void NestedParenthesesWithOperatorPrecedence()
    {
        TestMethodTemplate("(2 + 3) * (4 + 5) - (6 * 7) + (8 / 2)", 7);
    }
    
    [TestMethod]
    public void ExtremeLongExpression_ShouldEvaluateCorrectly()
    {
        var expected = System.Linq.Enumerable.Range(1, 100).Sum();
        var expr = string.Join(" + ", System.Linq.Enumerable.Range(1, 100).Select(i => i.ToString()));
        TestMethodTemplate(expr, expected);
    }
    
    [TestMethod]
    public void MultiplicationAndDivisionChain()
    {
        TestMethodTemplate("100 * 2 / 4 * 3 / 5", 30);
    }
    
    [TestMethod]
    public void AdditionAndSubtractionChain()
    {
        TestMethodTemplate("100 - 20 + 15 - 10 + 25 - 5", 105);
    }
    
    [TestMethod]
    public void ComplexNestedWithAllOperators()
    {
        TestMethodTemplate("((10 + 5) * 2 - (8 / 4)) + ((6 - 2) * (3 + 1))", 44);
    }
    
    [TestMethod]
    public void StressTest_CombinedComplexity()
    {
        TestMethodTemplate("((1+2+3+4+5) * 2) + ((6+7+8+9+10) * 2)", 110);
    }
}