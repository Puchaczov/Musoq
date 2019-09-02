using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Plugins;

namespace Musoq.Schema.System.Tests
{
    [TestClass]
    public class RangeTests
    {

        [TestMethod]
        public void RangeMaxTest()
        {
            var query = "select Value from #system.range(5)";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(5, table.Count);
            Assert.AreEqual(0L, table[0][0]);
            Assert.AreEqual(1L, table[1][0]);
            Assert.AreEqual(2L, table[2][0]);
            Assert.AreEqual(3L, table[3][0]);
            Assert.AreEqual(4L, table[4][0]);
        }

        [TestMethod]
        public void RangeMinMaxTest()
        {
            var query = "select Value from #system.range(1, 5)";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(4, table.Count);
            Assert.AreEqual(1L, table[0][0]);
            Assert.AreEqual(2L, table[1][0]);
            Assert.AreEqual(3L, table[2][0]);
            Assert.AreEqual(4L, table[3][0]);
        }


        [TestMethod]
        public void RangeMinSignedMaxTest()
        {
            var query = "select Value from #system.range(-1, 2)";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual(-1L, table[0][0]);
            Assert.AreEqual(0L, table[1][0]);
            Assert.AreEqual(1L, table[2][0]);
        }


        [TestMethod]
        public void RowNumberEvenForRangeMinSignedMaxTest()
        {
            var query = "select Value from #system.range(0, 5) where RowNumber() % 2 = 0";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual(1L, table[0][0]);
            Assert.AreEqual(3L, table[1][0]);
        }

        private CompiledQuery CreateAndRunVirtualMachine(string script)
        {
            return InstanceCreator.CompileForExecution(script, new SystemSchemaProvider());
        }

        static RangeTests()
        {
            new Environment().SetValue(Constants.NetStandardDllEnvironmentName, EnvironmentUtils.GetOrCreateEnvironmentVariable());
        }
    }
}
