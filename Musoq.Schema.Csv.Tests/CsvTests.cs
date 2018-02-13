using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Instructions;
using System.Linq;

namespace Musoq.Schema.Csv.Tests
{
    [TestClass]
    public class CsvTests
    {
        [TestMethod]
        public void SimpleCountTest()
        {
            var query = "SELECT Count(OperationDate) FROM #csv.file('./Files/BankingTransactions.csv')";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Count(OperationDate)", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(11, table[0].Values[0]);
        }

        [TestMethod]
        public void SimpleGroupByWithSum()
        {
            var query = "SELECT ParentCount(1), ExtractFromDate(OperationDate, 'month'), Count(OperationDate), SumIncome(ToDecimal(Money)), SumOutcome(ToDecimal(Money)), SumIncome(ToDecimal(Money)) - Abs(SumOutcome(ToDecimal(Money))) FROM #csv.file('./Files/BankingTransactions.csv') group by ExtractFromDate(OperationDate, 'month')";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Execute();

            Assert.AreEqual(6, table.Columns.Count());
            Assert.AreEqual("ParentCount(1)", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("ExtractFromDate(OperationDate, 'month')", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual("Count(OperationDate)", table.Columns.ElementAt(2).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);
            Assert.AreEqual("SumIncome(ToDecimal(Money))", table.Columns.ElementAt(3).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(3).ColumnType);
            Assert.AreEqual("SumOutcome(ToDecimal(Money))", table.Columns.ElementAt(4).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(4).ColumnType);
            Assert.AreEqual("SumIncome(ToDecimal(Money)) - Abs(SumOutcome(ToDecimal(Money)))", table.Columns.ElementAt(5).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(5).ColumnType);

            Assert.AreEqual(2, table.Count);

            Assert.AreEqual(11, table[0].Values[0]);
            Assert.AreEqual(1, table[0].Values[1]);
            Assert.AreEqual(6, table[0].Values[2]);
            Assert.AreEqual(4199m, table[0].Values[3]);
            Assert.AreEqual(197.15m, table[0].Values[4]);
            Assert.AreEqual(4001.85m, table[0].Values[5]);
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
            return new CsvSchema();
        }
    }
}
