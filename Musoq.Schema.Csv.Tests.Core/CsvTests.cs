using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Schema.Csv.Tests.Core
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
            var query =
                "SELECT ParentCount(1), ExtractFromDate(OperationDate, 'month'), Count(OperationDate), SumIncome(ToDecimal(Money)), SumOutcome(ToDecimal(Money)), SumIncome(ToDecimal(Money)) - Abs(SumOutcome(ToDecimal(Money))) FROM #csv.file('./Files/BankingTransactions.csv', ',') group by ExtractFromDate(OperationDate, 'month')";

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
            Assert.AreEqual("SumIncome(ToDecimal(Money)) - Abs(SumOutcome(ToDecimal(Money)))",
                table.Columns.ElementAt(5).Name);
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

        [TestMethod]
        public void InneJoinTest()
        {
            var query = "select persons.Name, persons.Surname, grades.Subject, grades.ToDecimal(grades.Grade) from #csv.file('./Files/Persons.csv', ',') persons inner join #csv.file('./Files/Gradebook.csv', ',') grades on persons.Id = grades.PersonId";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(4, table.Columns.Count());

            Assert.AreEqual("persons.Name", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("persons.Surname", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual("grades.Subject", table.Columns.ElementAt(2).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);
            Assert.AreEqual("ToDecimal(grades.Grade)", table.Columns.ElementAt(3).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(3).ColumnType);

            Assert.AreEqual(24, table.Count);

            Assert.AreEqual("Jan", table[0][0]);
            Assert.AreEqual("Grzyb", table[0][1]);
            Assert.AreEqual("Math", table[0][2]);
            Assert.AreEqual(5m, table[0][3]);

            Assert.AreEqual("Jan", table[1][0]);
            Assert.AreEqual("Grzyb", table[1][1]);
            Assert.AreEqual("English", table[1][2]);
            Assert.AreEqual(2m, table[1][3]);

            Assert.AreEqual("Jan", table[2][0]);
            Assert.AreEqual("Grzyb", table[2][1]);
            Assert.AreEqual("Biology", table[2][2]);
            Assert.AreEqual(4m, table[2][3]);

            Assert.AreEqual("Jan", table[3][0]);
            Assert.AreEqual("Grzyb", table[3][1]);
            Assert.AreEqual("Math", table[3][2]);
            Assert.AreEqual(4m, table[3][3]);

            Assert.AreEqual("Jan", table[4][0]);
            Assert.AreEqual("Grzyb", table[4][1]);
            Assert.AreEqual("Biology", table[4][2]);
            Assert.AreEqual(3m, table[4][3]);

            Assert.AreEqual("Jan", table[5][0]);
            Assert.AreEqual("Grzyb", table[5][1]);
            Assert.AreEqual("Math", table[5][2]);
            Assert.AreEqual(4m, table[5][3]);

            Assert.AreEqual("Marek", table[6][0]);
            Assert.AreEqual("Tarczynski", table[6][1]);
            Assert.AreEqual("Math", table[6][2]);
            Assert.AreEqual(5m, table[6][3]);

            Assert.AreEqual("Marek", table[7][0]);
            Assert.AreEqual("Tarczynski", table[7][1]);
            Assert.AreEqual("English", table[7][2]);
            Assert.AreEqual(2m, table[7][3]);

            Assert.AreEqual("Witek", table[8][0]);
            Assert.AreEqual("Lechoń", table[8][1]);
            Assert.AreEqual("Biology", table[8][2]);
            Assert.AreEqual(4m, table[8][3]);

            Assert.AreEqual("Witek", table[9][0]);
            Assert.AreEqual("Lechoń", table[9][1]);
            Assert.AreEqual("Math", table[9][2]);
            Assert.AreEqual(4m, table[9][3]);

            Assert.AreEqual("Witek", table[10][0]);
            Assert.AreEqual("Lechoń", table[10][1]);
            Assert.AreEqual("Biology", table[10][2]);
            Assert.AreEqual(3m, table[10][3]);

            Assert.AreEqual("Witek", table[11][0]);
            Assert.AreEqual("Lechoń", table[11][1]);
            Assert.AreEqual("Math", table[11][2]);
            Assert.AreEqual(4m, table[11][3]);

            Assert.AreEqual("Anna", table[12][0]);
            Assert.AreEqual("Rozmaryn", table[12][1]);
            Assert.AreEqual("Math", table[12][2]);
            Assert.AreEqual(5m, table[12][3]);

            Assert.AreEqual("Anna", table[13][0]);
            Assert.AreEqual("Rozmaryn", table[13][1]);
            Assert.AreEqual("English", table[13][2]);
            Assert.AreEqual(2m, table[13][3]);

            Assert.AreEqual("Anna", table[14][0]);
            Assert.AreEqual("Rozmaryn", table[14][1]);
            Assert.AreEqual("Biology", table[14][2]);
            Assert.AreEqual(4m, table[14][3]);

            Assert.AreEqual("Anna", table[15][0]);
            Assert.AreEqual("Rozmaryn", table[15][1]);
            Assert.AreEqual("Math", table[15][2]);
            Assert.AreEqual(4m, table[15][3]);

            Assert.AreEqual("Anna", table[16][0]);
            Assert.AreEqual("Rozmaryn", table[16][1]);
            Assert.AreEqual("Biology", table[16][2]);
            Assert.AreEqual(3m, table[16][3]);

            Assert.AreEqual("Anna", table[17][0]);
            Assert.AreEqual("Rozmaryn", table[17][1]);
            Assert.AreEqual("Math", table[17][2]);
            Assert.AreEqual(4m, table[17][3]);

            Assert.AreEqual("Anna", table[18][0]);
            Assert.AreEqual("Trzpień", table[18][1]);
            Assert.AreEqual("Math", table[18][2]);
            Assert.AreEqual(5m, table[18][3]);

            Assert.AreEqual("Anna", table[19][0]);
            Assert.AreEqual("Trzpień", table[19][1]);
            Assert.AreEqual("English", table[19][2]);
            Assert.AreEqual(2m, table[19][3]);

            Assert.AreEqual("Anna", table[20][0]);
            Assert.AreEqual("Trzpień", table[20][1]);
            Assert.AreEqual("Biology", table[20][2]);
            Assert.AreEqual(4m, table[20][3]);

            Assert.AreEqual("Anna", table[21][0]);
            Assert.AreEqual("Trzpień", table[21][1]);
            Assert.AreEqual("Math", table[21][2]);
            Assert.AreEqual(4m, table[21][3]);

            Assert.AreEqual("Anna", table[22][0]);
            Assert.AreEqual("Trzpień", table[22][1]);
            Assert.AreEqual("Biology", table[22][2]);
            Assert.AreEqual(3m, table[22][3]);

            Assert.AreEqual("Anna", table[23][0]);
            Assert.AreEqual("Trzpień", table[23][1]);
            Assert.AreEqual("Math", table[23][2]);
            Assert.AreEqual(4m, table[23][3]);
        }

        private CompiledQuery CreateAndRunVirtualMachine(string script)
        {
            return InstanceCreator.CompileForExecution(script, new CsvSchemaProvider());
        }
    }
}