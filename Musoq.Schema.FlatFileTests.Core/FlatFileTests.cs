using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Plugins;
using Musoq.Schema.FlatFile;

namespace Musoq.Schema.FlatFileTests.Core
{
    [TestClass]
    public class FlatFileTests
    {
        [TestMethod]
        public void HasSelectedAllLinesTest()
        {
            var query = @"select LineNumber, Line from #FlatFile.file('./TestMultilineFile.txt')";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("LineNumber", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("Line", table.Columns.ElementAt(1).ColumnName);
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

        [TestMethod]
        public void FlatFileSource_CancelledLoadTest()
        {
            var endWorkTokenSource = new CancellationTokenSource();
            endWorkTokenSource.Cancel();
            var schema = new FlatFileSource("./TestMultilineFile.txt", new RuntimeContext(endWorkTokenSource.Token, new ISchemaColumn[0]));

            int fires = 0;
            foreach (var item in schema.Rows)
                fires += 1;

            Assert.AreEqual(0, fires);
        }

        [TestMethod]
        public void FlatFileSource_FullLoadTest()
        {
            var schema = new FlatFileSource("./TestMultilineFile.txt", RuntimeContext.Empty);

            int fires = 0;
            foreach (var item in schema.Rows)
                fires += 1;

            Assert.AreEqual(6, fires);
        }

        private CompiledQuery CreateAndRunVirtualMachine(string script)
        {
            return InstanceCreator.CompileForExecution(script, new FlatFileSchemaProvider());
        }

        static FlatFileTests()
        {
            new Environment().SetValue(Constants.NetStandardDllEnvironmentName, EnvironmentUtils.GetOrCreateEnvironmentVariable());
        }
    }
}