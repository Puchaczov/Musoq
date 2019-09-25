using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;
using Musoq.Plugins;
using Musoq.Schema.System;

namespace Musoq.Converter.Tests
{
    [TestClass]
    public class BuildTests
    {
        [TestMethod]
        public void CompileForStoreTest()
        {
            var query = "select 1 from #system.dual()";

            var (DllFile, PdbFile) = CreateForStore(query);

            Assert.IsNotNull(DllFile);
            Assert.IsNotNull(PdbFile);

            Assert.AreNotEqual(0, DllFile.Length);
            Assert.AreNotEqual(0, PdbFile.Length);
        }

        [TestMethod]
        public async Task CompileForStoreAsyncTest()
        {
            var query = "select 1 from #system.dual()";

            var arrays = await InstanceCreator.CompileForStoreAsync(query, Guid.NewGuid().ToString(), new SystemSchemaProvider());

            Assert.IsNotNull(arrays.DllFile);
            Assert.IsNotNull(arrays.PdbFile);

            Assert.AreNotEqual(0, arrays.DllFile.Length);
            Assert.AreNotEqual(0, arrays.PdbFile.Length);
        }

        private (byte[] DllFile, byte[] PdbFile) CreateForStore(string script)
        {
            return InstanceCreator.CompileForStore(script, Guid.NewGuid().ToString(), new SystemSchemaProvider());
        }

        static BuildTests()
        {
            new Musoq.Plugins.Environment().SetValue(Constants.NetStandardDllEnvironmentName, EnvironmentUtils.GetOrCreateEnvironmentVariable());
        }
    }
}
