using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using System.Linq;
using Musoq.Evaluator;

namespace Musoq.Schema.Csv.Tests
{
    [TestClass]
    public class CsvTests
    {
        [TestMethod]
        public void SimpleSelectTest()
        {
            var query = "SELECT Name FROM #csv.file('./Files/BankingTransactions.csv', ',')";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(11, table.Count);
            Assert.AreEqual("Salary", table[0].Values[0]);
            Assert.AreEqual("Restaurant A", table[1].Values[0]);
            Assert.AreEqual("Bus ticket", table[2].Values[0]);
            Assert.AreEqual("Tesco", table[3].Values[0]);
            Assert.AreEqual("Restaurant B", table[4].Values[0]);
            Assert.AreEqual("Service", table[5].Values[0]);
            Assert.AreEqual("Salary", table[6].Values[0]);
            Assert.AreEqual("Restaurant A", table[7].Values[0]);
            Assert.AreEqual("Bus ticket", table[8].Values[0]);
            Assert.AreEqual("Tesco", table[9].Values[0]);
            Assert.AreEqual("Restaurant B", table[10].Values[0]);
        }

        [TestMethod]
        public void SimpleCountTest()
        {
            var query = "SELECT Count(OperationDate) FROM #csv.file('./Files/BankingTransactions.csv', ',')";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Count(OperationDate)", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(11, table[0].Values[0]);
        }

        [TestMethod]
        public void SimpleGroupByWithSum()
        {
            var query = "SELECT ParentCount(1), ExtractFromDate(OperationDate, 'month'), Count(OperationDate), SumIncome(ToDecimal(Money)), SumOutcome(ToDecimal(Money)), SumIncome(ToDecimal(Money)) - Abs(SumOutcome(ToDecimal(Money))) FROM #csv.file('./Files/BankingTransactions.csv', ',') group by ExtractFromDate(OperationDate, 'month')";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

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
            Assert.AreEqual(-157.15m, table[0].Values[4]);
            Assert.AreEqual(4041.85m, table[0].Values[5]);

            Assert.AreEqual(11, table[1].Values[0]);
            Assert.AreEqual(2, table[1].Values[1]);
            Assert.AreEqual(5, table[1].Values[2]);
            Assert.AreEqual(4000m, table[1].Values[3]);
            Assert.AreEqual(-157.15m, table[1].Values[4]);
            Assert.AreEqual(3842.85m, table[1].Values[5]);
        }

        private IRunnable CreateAndRunVirtualMachine(string script)
        {
            return InstanceCreator.Create(script, new CsvSchemaProvider());
        }
    }
}
