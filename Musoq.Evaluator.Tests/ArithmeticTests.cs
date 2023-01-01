using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class ArithmeticTests : BasicEntityTestBase
    {
        [TestMethod]
        public void MultiDividingTest()
        {
            TestMethodTemplate<long>("256 / 8 / 8", 4);
        }

        [TestMethod]
        public void DivisionWithAdditionTest()
        {
            TestMethodTemplate<long>("256 + 256 / 2", 384);
        }

        [TestMethod]
        public void DivisionWithAdditionHigherPriorityTest()
        {
            TestMethodTemplate<long>("(256 + 256) / 2", 256);
        }

        [TestMethod]
        public void DivisionWithMultiplicationTest()
        {
            TestMethodTemplate<long>("2 * 3 / 2", 3);
        }

        [TestMethod]
        public void DivisionWithSubtraction1Test()
        {
            TestMethodTemplate<long>("128 / 64 - 2", 0);
        }

        [TestMethod]
        public void DivisionWithSubtraction2Test()
        {
            TestMethodTemplate<long>("128 - 64 / 2", 96);
        }

        [TestMethod]
        public void MultiSubtractionTest()
        {
            TestMethodTemplate<long>("256 - 128 - 128", 0);
        }

        [TestMethod]
        public void SubtractionWithAdditionTest()
        {
            TestMethodTemplate<long>("256 + 128 - 128", 256);
        }

        [TestMethod]
        public void SubtractionWithUnaryMinusTest()
        {
            TestMethodTemplate<long>("1 - -1", 2);
        }

        [TestMethod]
        public void SubtractionWithUnaryMinusExpressionTest()
        {
            TestMethodTemplate<long>("1 - -(1 + 2)", 4);
        }

        [TestMethod]
        public void SubtractionWithUnaryMinusAndAdditionTest()
        {
            TestMethodTemplate<long>("1 - -1 + 2", 4);
        }

        [TestMethod]
        public void SubtractionWithUnaryMinusAndSubtractionTest()
        {
            TestMethodTemplate<long>("1 - (-1) - 2", 0);
        }

        [TestMethod]
        public void AdditionWithUnaryMinusAndAdditionTest()
        {
            TestMethodTemplate<long>("1 + -1 + 2", 2);
        }

        [TestMethod]
        public void ModuloExpressionWithMultiplicationTest()
        {
            TestMethodTemplate<long>("8 % 3 * 2", 4);
        }

        [TestMethod]
        public void ModuloExpressionWithSubtractionTest()
        {
            TestMethodTemplate<long>("8 % 3 - 2", 0);
        }

        [TestMethod]
        public void ModuloExpressionWithAdditionTest()
        {
            TestMethodTemplate<long>("8 % 3 + 2", 4);
        }

        [TestMethod]
        public void MultiModuloExpressionTest()
        {
            TestMethodTemplate<long>("5 % 4 % 6", 1);
        }

        [TestMethod]
        public void ComplexArithmeticExpression1Test()
        {
            TestMethodTemplate<long>("1 + 2 * 3 * ( 7 * 8 ) - ( 45 - 10 )", 302);
        }

        [TestMethod]
        public void CaseWhenArithmeticExpressionTest()
        {
            TestMethodTemplate<long>("1 + (case when 2 > 1 then 1 else 0 end) - 1", 1);
        }
    }
}