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
}