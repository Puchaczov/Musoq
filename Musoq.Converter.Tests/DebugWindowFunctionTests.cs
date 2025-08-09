using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Tests.Common;
using System;

namespace Musoq.Converter.Tests
{
    [TestClass]
    public class DebugWindowFunctionTests
    {
        [TestMethod]
        public void Debug_BasicRankFunction_ShouldWork()
        {
            // Test without OVER clause
            var query = @"select Dummy, RANK() from #system.dual()";
            
            var (dllFile, pdbFile) = CreateForStore(query);
            
            Assert.IsNotNull(dllFile);
            Assert.IsNotNull(pdbFile);
        }
        
        [TestMethod]  
        public void Debug_RankFunctionWithOver_ShouldWork()
        {
            // Test with empty OVER clause
            var query = @"select Dummy, RANK() OVER () from #system.dual()";
            
            var (dllFile, pdbFile) = CreateForStore(query);
            
            Assert.IsNotNull(dllFile);
            Assert.IsNotNull(pdbFile);
        }
        
        [TestMethod]  
        public void Debug_RankFunctionWithOrderBy_ShouldWork()
        {
            // Test with OVER clause containing ORDER BY
            var query = @"select Dummy, RANK() OVER (ORDER BY Dummy) from #system.dual()";
            
            var (dllFile, pdbFile) = CreateForStore(query);
            
            Assert.IsNotNull(dllFile);
            Assert.IsNotNull(pdbFile);
        }
        
        private static (byte[] dllFile, byte[] pdbFile) CreateForStore(string query)
        {
            return InstanceCreator.CompileForStore(query, Guid.NewGuid().ToString(), 
                new SystemSchemaProvider(), new TestsLoggerResolver());
        }
    }
}