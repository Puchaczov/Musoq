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
