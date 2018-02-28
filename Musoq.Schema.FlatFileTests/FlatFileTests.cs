using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Instructions;
using Musoq.Schema.FlatFile;

namespace Musoq.Schema.FlatFileTests
{
    [TestClass]
    public class FlatFileTests
    {
        [TestMethod]
        public void HasSelectedAllLinesTest()
        {
            var query = @"select LineNumber, Line from #FlatFile.whatever('./TestMultilineFile.txt')";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Execute();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("LineNumber", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("Line", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);

            Assert.AreEqual(6, table.Count);

            Assert.AreEqual(1, table[0].Values[0]);
            Assert.AreEqual(string.Empty, table[0].Values[1]);

            Assert.AreEqual(2, table[1].Values[0]);
            Assert.AreEqual("line 2", table[1].Values[1]);

            Assert.AreEqual(3, table[2].Values[0]);
            Assert.AreEqual("line3", table[2].Values[1]);

            Assert.AreEqual(4, table[3].Values[0]);
            Assert.AreEqual("line", table[3].Values[1]);

            Assert.AreEqual(5, table[4].Values[0]);
            Assert.AreEqual(string.Empty, table[4].Values[1]);

            Assert.AreEqual(6, table[5].Values[0]);
            Assert.AreEqual("linexx", table[5].Values[1]);
        }

        private IVirtualMachine CreateAndRunVirtualMachine(string script)
        {
            return InstanceCreator.Create(script, new FlatFileSchemaProvider());
        }
    }
}
