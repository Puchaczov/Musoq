using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class KeyDebugTest : BasicEntityTestBase
    {
        protected CompiledQuery CreateAndRunVirtualMachine<T>(
            string script,
            IDictionary<string, IEnumerable<T>> sources)
            where T : SalesEntity
        {
            return InstanceCreator.CompileForExecution(
                script, 
                Guid.NewGuid().ToString(), 
                new SalesSchemaProvider<T>(sources),
                LoggerResolver);
        }

        [TestMethod]
        public void MinimalQuery_ShouldWork()
        {
            // Simplest possible query to debug key issue
            var query = @"SELECT Category FROM #A.entities()";
            var sources = new Dictionary<string, IEnumerable<SalesEntity>>
            {
                {
                    "#A", new[]
                    {
                        new SalesEntity("Books", "Book1", 10, 100m)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();
            
            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("Books", table[0].Values[0]);
        }
    }
}