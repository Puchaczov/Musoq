using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Instructions;

namespace Musoq.Schema.Zip.Tests
{
    [TestClass]
    public class ZipTests
    {
        [TestMethod]
        public void SimpleZipSelectTest()
        {
            var query = @"select FullName from #zip.file('./Files.zip')";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("FullName", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("Files/File1.txt", table[0].Values[0]);
            Assert.AreEqual("Files/File2.txt", table[1].Values[0]);
            Assert.AreEqual("Files/SubFolder/File3.txt", table[2].Values[0]);
        }

        [TestMethod]
        public void DecompressTest()
        {
            var query = "select Decompress(AggregateFiles(File), './Results') from #zip.file('./Files.zip')";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Decompress(AggregateFiles(File), './Results')", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("./Results", table[0].Values[0]);

            Assert.IsTrue(File.Exists("./Results/File1.txt"));
            Assert.IsTrue(File.Exists("./Results/File2.txt"));
            Assert.IsTrue(File.Exists("./Results/SubFolder/File3.txt"));

            Directory.Delete("./Results", true);
        }

        private IVirtualMachine CreateAndRunVirtualMachine(string script)
        {
            return InstanceCreator.Create(script, new ZipSchemaProvider());
        }
    }

    internal class ZipSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new ZipSchema(schema);
        }
    }
}
