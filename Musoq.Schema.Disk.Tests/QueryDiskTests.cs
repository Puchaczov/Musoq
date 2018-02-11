using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Instructions;
using Musoq.Schema.Disk.Disk;

namespace Musoq.Schema.Disk.Tests
{
    [TestClass]
    public class QueryDiskTests
    {
        [TestMethod]
        public void CompressFilesTests()
        {
            var query = $"select Compress(AggregateFiles(), './Results/{nameof(CompressFilesTests)}.zip', 'fastest') from #disk.directory('./Files', 'false')";
            
            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual($"Compress(AggregateFiles(), './Results/{nameof(CompressFilesTests)}.zip', 'fastest')", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual($"./Results/{nameof(CompressFilesTests)}.zip", table[0].Values[0]);
            
            File.Delete($"./Results/{nameof(CompressFilesTests)}.zip");
        }

        private IVirtualMachine CreateAndRunVirtualMachine(string script)
        {
            return InstanceCreator.Create(script, new JsonSchemaProvider());
        }
    }

    internal class JsonSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new DiskSchema();
        }
    }
}
