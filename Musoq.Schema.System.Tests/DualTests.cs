using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Plugins;

namespace Musoq.Schema.System.Tests
{
    [TestClass]
    public class DualTests
    {
        [TestMethod]
        public void SimpleDualTest()
        {
            var query = "select Dummy from #system.dual()";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("single", table[0][0]);
        }

        [TestMethod]
        public void SimpleComputedDualTest()
        {
            var query = "select 2 + 1 from #system.dual()";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(3L, table[0][0]);
        }

        [TestMethod]
        public void SomeTest()
        {
            var query = "select ToDecimal(1 + 2) / 5 from #system.dual()";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(0.6m, table[0][0]);
        }

        [TestMethod]
        public void UnionTest()
        {
            var query = "select 1 as t from #system.dual() union (t) select 2 as t from #system.dual()";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual(1, table[0][0]);
            Assert.AreEqual(2, table[1][0]);
        }

        private CompiledQuery CreateAndRunVirtualMachine(string script)
        {
            return InstanceCreator.CompileForExecution(script, new SystemSchemaProvider());
        }

        static DualTests()
        {
            new Environment().SetValue(Constants.NetStandardDllEnvironmentName, EnvironmentUtils.GetOrCreateEnvironmentVariable());
        }
    }
}
