using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class ArithmeticTests : TestBase
    {
        [TestMethod]
        public void MultiDividingTest()
            => TestMethodTemplate<long>("256 / 8 / 8", 4);

        [TestMethod]
        public void DivisionWithAdditionTest()
            => TestMethodTemplate<long>("256 + 256 / 2", 384);

        [TestMethod]
        public void DivisionWithAdditionHigherPriorityTest()
            => TestMethodTemplate<long>("(256 + 256) / 2", 256);

        [TestMethod]
        public void DivisionWithMultiplicationTest()
            => TestMethodTemplate<long>("2 * 3 / 2", 3);

        [TestMethod]
        public void DivisionWithSubtraction1Test()
            => TestMethodTemplate<long>("128 / 64 - 2", 0);

        [TestMethod]
        public void DivisionWithSubtraction2Test()
            => TestMethodTemplate<long>("128 - 64 / 2", 96);

        [TestMethod]
        public void MultiSubtractionTest()
            => TestMethodTemplate<long>("256 - 128 - 128", 0);

        [TestMethod]
        public void SubtractionWithAdditionTest()
            => TestMethodTemplate<long>("256 + 128 - 128", 256);

        [TestMethod]
        public void SubtractionWithUnaryMinusTest()
            => TestMethodTemplate<long>("1 - -1", 2);

        [TestMethod]
        public void SubtractionWithUnaryMinusExpressionTest()
            => TestMethodTemplate<long>("1 - -(1 + 2)", 4);

        [TestMethod]
        public void SubtractionWithUnaryMinusAndAdditionTest()
            => TestMethodTemplate<long>("1 - -1 + 2", 4);

        [TestMethod]
        public void ModuloExpressionWithMultiplicationTest()
            => TestMethodTemplate<long>("8 % 3 * 2", 4);

        [TestMethod]
        public void ModuloExpressionWithSubtractionTest()
            => TestMethodTemplate<long>("8 % 3 - 2", 0);

        [TestMethod]
        public void ModuloExpressionWithAdditionTest()
            => TestMethodTemplate<long>("8 % 3 + 2", 4);

        private void TestMethodTemplate<TResult>(string operation, TResult score)
        {
            var query = $"select {operation} from #A.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("ABCAACBA")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(typeof(TResult), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(score, table[0][0]);
        }
    }
}
