using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Instructions;

namespace Musoq.Schema.Zip
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

            Assert.AreEqual(5, table.Count);
            Assert.AreEqual("Files/", table[0].Values[0]);
            Assert.AreEqual("Files/File1.txt", table[1].Values[0]);
            Assert.AreEqual("Files/File2.txt", table[2].Values[0]);
            Assert.AreEqual("Files/SubFolder/", table[3].Values[0]);
            Assert.AreEqual("Files/SubFolder/File3.txt", table[4].Values[0]);
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
