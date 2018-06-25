using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;

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

        private CompiledQuery CreateAndRunVirtualMachine(string script)
        {
            return InstanceCreator.Create(script, new SystemSchemaProvider());
        }
    }
}
