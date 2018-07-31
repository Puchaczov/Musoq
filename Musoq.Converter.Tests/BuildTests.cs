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

            var arrays = CreateForStore(query);

            Assert.IsNotNull(arrays.DllFile);
            Assert.IsNotNull(arrays.PdbFile);

            Assert.AreNotEqual(0, arrays.DllFile.Length);
            Assert.AreNotEqual(0, arrays.PdbFile.Length);
        }

        private (byte[] DllFile, byte[] PdbFile) CreateForStore(string script)
        {
            return InstanceCreator.CompileForStore(script, new SystemSchemaProvider());
        }

        static BuildTests()
        {
            new Environment().SetValue(Constants.NetStandardDllEnvironmentName, EnvironmentUtils.GetOrCreateEnvironmentVariable());
        }
    }
}
