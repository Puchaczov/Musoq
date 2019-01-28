using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using Musoq.Plugins;
using Environment = Musoq.Plugins.Environment;

namespace Musoq.Schema.Csv.Tests.Core
{
    [TestClass]
    public class CsvTests
    {
        [TestMethod]
        public void ReplaceNotValidCharacters()
        {
            var columnName = CsvHelper.MakeHeaderNameValidColumnName("#Column name 123 22@");

            Assert.AreEqual("ColumnName12322", columnName);
        }

        [TestMethod]
        public void SimpleSelectWithSkipLinesTest()
        {
            var query = "SELECT Name FROM #csv.file('./Files/BankingTransactionsWithSkippedLines.csv', ',', true, 2)";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
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
        public void SimpleSelectWithCouplingTableSyntaxSkipLinesTest()
        {
            var query = "" +
                "table CsvFile {" +
                "   Name 'System.String'" +
                "};" +
                "couple #csv.file with table CsvFile as SourceCsvFile;" +
                "select Name from SourceCsvFile('./Files/BankingTransactionsWithSkippedLines.csv', ',', true, 2);";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
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
        public void CheckTypesTest()
        {
            var query = "" +
                "table Persons {" +
                "   Id 'System.Int32'," +
                "   Name 'System.String'" +
                "};" +
                "couple #csv.file with table Persons as SourceOfPersons;" +
                "select Id, Name from SourceOfPersons('./Files/Persons.csv', ',', true, 0)";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("Id", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual(typeof(int?), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("Name", table.Columns.ElementAt(1).ColumnName);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);

            Assert.AreEqual(5, table.Count);
            Assert.AreEqual(1, table[0].Values[0]);
            Assert.AreEqual("Jan", table[0].Values[1]);
            Assert.AreEqual(2, table[1].Values[0]);
            Assert.AreEqual("Marek", table[1].Values[1]);
            Assert.AreEqual(3, table[2].Values[0]);
            Assert.AreEqual("Witek", table[2].Values[1]);
            Assert.AreEqual(4, table[3].Values[0]);
            Assert.AreEqual("Anna", table[3].Values[1]);
            Assert.AreEqual(5, table[4].Values[0]);
            Assert.AreEqual("Anna", table[4].Values[1]);
        }

        [TestMethod]
        public void CheckNullValues()
        {
            var query = "" +
                "table BankingTransactions {" +
                "   Category 'string'," +
                "   Money 'decimal'" +
                "};" +
                "couple #csv.file with table BankingTransactions as SourceOfBankingTransactions;" +
                "select Category, Money from SourceOfBankingTransactions('./Files/BankingTransactionsNullValues.csv', ',', true, 0) where Category is null or Money is null;";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("Category", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("Money", table.Columns.ElementAt(1).ColumnName);
            Assert.AreEqual(typeof(decimal?), table.Columns.ElementAt(1).ColumnType);

            Assert.AreEqual(4, table.Count);
            Assert.AreEqual("Life", table[0].Values[0]);
            Assert.AreEqual(null, table[0].Values[1]);
            Assert.AreEqual(null, table[1].Values[0]);
            Assert.AreEqual(-1m, table[1].Values[1]);
            Assert.AreEqual(null, table[2].Values[0]);
            Assert.AreEqual(-121.95m, table[2].Values[1]);
            Assert.AreEqual(null, table[3].Values[0]);
            Assert.AreEqual(null, table[3].Values[1]);
        }

        [TestMethod]
        public void SimpleSelectWithCouplingTableSyntaxSkipLinesTest2()
        {
            var query = "" +
                "table CsvFile {" +
                "   Name 'System.String'" +
                "};" +
                "couple #csv.file with table CsvFile as SourceCsvFile;" +
                "with FilesToScan as (" +
                "   select './Files/BankingTransactionsWithSkippedLines.csv', ',', true, 2 from #csv.empty()" +
                ")" +
                "select Name from SourceCsvFile(FilesToScan);";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
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
        public void SameFileReadDoubleTimesTest()
        {
            var query = "" +
                "table CsvFile {" +
                "   Name 'System.String'" +
                "};" +
                "couple #csv.file with table CsvFile as SourceCsvFile;" +
                "with FilesToScan as (" +
                "   select './Files/BankingTransactionsWithSkippedLines.csv' as FileName, ',', true, 2 from #csv.empty()" +
                "   union all (FileName) " +
                "   select './Files/BankingTransactionsWithSkippedLines.csv' as FileName, ',', true, 2 from #csv.empty()" +
                ")" +
                "select Name from SourceCsvFile(FilesToScan);";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(22, table.Count);
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

            Assert.AreEqual("Salary", table[11].Values[0]);
            Assert.AreEqual("Restaurant A", table[12].Values[0]);
            Assert.AreEqual("Bus ticket", table[13].Values[0]);
            Assert.AreEqual("Tesco", table[14].Values[0]);
            Assert.AreEqual("Restaurant B", table[15].Values[0]);
            Assert.AreEqual("Service", table[16].Values[0]);
            Assert.AreEqual("Salary", table[17].Values[0]);
            Assert.AreEqual("Restaurant A", table[18].Values[0]);
            Assert.AreEqual("Bus ticket", table[19].Values[0]);
            Assert.AreEqual("Tesco", table[20].Values[0]);
            Assert.AreEqual("Restaurant B", table[21].Values[0]);
        }

        [TestMethod]
        public void SimpleSelectTest()
        {
            var query = "SELECT Name FROM #csv.file('./Files/BankingTransactions.csv', ',', true, 0)";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
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
        public void SimpleSelectNoHeaderTest()
        {
            var query = "SELECT Column3 FROM #csv.file('./Files/BankingTransactionsNoHeader.csv', ',', false, 0)";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Column3", table.Columns.ElementAt(0).ColumnName);
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
            var query = "SELECT Count(OperationDate) FROM #csv.file('./Files/BankingTransactions.csv', ',', true, 0)";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Count(OperationDate)", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(11, table[0].Values[0]);
        }

        [TestMethod]
        public void SimpleGroupByWithSum()
        {
            var query =
                @"
select 
    Count(OperationDate, 1), 
    ExtractFromDate(OperationDate, 'month'), 
    Count(OperationDate), 
    SumIncome(ToDecimal(Money)), 
    SumOutcome(ToDecimal(Money)), 
    SumIncome(ToDecimal(Money)) - Abs(SumOutcome(ToDecimal(Money))) 
from #csv.file('./Files/BankingTransactions.csv', ',', true, 0) 
group by ExtractFromDate(OperationDate, 'month')";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(6, table.Columns.Count());
            Assert.AreEqual("Count(OperationDate, 1)", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("ExtractFromDate(OperationDate, 'month')", table.Columns.ElementAt(1).ColumnName);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual("Count(OperationDate)", table.Columns.ElementAt(2).ColumnName);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);
            Assert.AreEqual("SumIncome(ToDecimal(Money))", table.Columns.ElementAt(3).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(3).ColumnType);
            Assert.AreEqual("SumOutcome(ToDecimal(Money))", table.Columns.ElementAt(4).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(4).ColumnType);
            Assert.AreEqual("SumIncome(ToDecimal(Money)) - Abs(SumOutcome(ToDecimal(Money)))",
                table.Columns.ElementAt(5).ColumnName);
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
        public void InnerJoinTest()
        {
            var query = @"
select 
    persons.Name, 
    persons.Surname, 
    grades.Subject, 
    grades.ToDecimal(grades.Grade) 
from #csv.file('./Files/Persons.csv', ',', true, 0) persons 
inner join #csv.file('./Files/Gradebook.csv', ',', true, 0) grades on persons.Id = grades.PersonId";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(4, table.Columns.Count());

            Assert.AreEqual("persons.Name", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("persons.Surname", table.Columns.ElementAt(1).ColumnName);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual("grades.Subject", table.Columns.ElementAt(2).ColumnName);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);
            Assert.AreEqual("ToDecimal(grades.Grade)", table.Columns.ElementAt(3).ColumnName);
            Assert.AreEqual(typeof(decimal?), table.Columns.ElementAt(3).ColumnType);

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

        [TestMethod]
        public void RichStatsFakeBankFile1Test()
        {
            var query = @"
with BasicIndicators as (
	select 
		ExtractFromDate(DateTime, 'month') as 'Month', 
		ClusteredByContainsKey('./Files/Categories.txt', ChargeName) as 'Category', 
		SumIncome(ToDecimal(Amount)) as Income, 
		SumIncome(ToDecimal(Amount), 1) as 'MonthlyIncome',
		Round(PercentOf(Abs(SumOutcome(ToDecimal(Amount))), SumIncome(ToDecimal(Amount), 1)), 2) as 'PercOfOutForOvInc',	
		SumOutcome(ToDecimal(Amount)) as Outcome, 
		SumOutcome(ToDecimal(Amount), 1) as 'MonthlyOutcome',
		SumIncome(ToDecimal(Amount), 1) + SumOutcome(ToDecimal(Amount), 1) as 'MoneysLeft',
		SumIncome(ToDecimal(Amount), 2) + SumOutcome(ToDecimal(Amount), 2) as 'OvMoneysLeft'
	from #csv.file('./Files/FakeBankingTransactions.csv', ',', true, 0) as csv
	group by 
		ExtractFromDate(DateTime, 'month'), 
		ClusteredByContainsKey('./Files/Categories.txt', ChargeName)
), AggregatedCategories as (
	select Category, Sum(Outcome) as 'CategoryOutcome' from BasicIndicators group by Category
)
select
	bi.Month as Month,
	bi.Category as Category,
	bi.Income as Income,
	bi.MonthlyIncome as 'Monthly Income',
	bi.PercOfOutForOvInc as '% Of Out. for ov. inc.',
	bi.Outcome as Outcome,
	bi.MonthlyOutcome as 'Monthly Outcome',
	bi.MoneysLeft as 'Moneys Left',
	bi.OvMoneysLeft as 'Ov. Moneys Left',
	ac.CategoryOutcome as 'Ov. Categ. Outcome',
    ((bi.MonthlyIncome - bi.MonthlyOutcome) / bi.MonthlyIncome) as 'Saving Coeff'
from BasicIndicators bi inner join AggregatedCategories ac on bi.Category = ac.Category";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(11, table.Columns.Count());

            Assert.AreEqual("Month", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnIndex);

            Assert.AreEqual("Category", table.Columns.ElementAt(1).ColumnName);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual(1, table.Columns.ElementAt(1).ColumnIndex);

            Assert.AreEqual("Income", table.Columns.ElementAt(2).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(2).ColumnType);
            Assert.AreEqual(2, table.Columns.ElementAt(2).ColumnIndex);

            Assert.AreEqual("Monthly Income", table.Columns.ElementAt(3).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(3).ColumnType);
            Assert.AreEqual(3, table.Columns.ElementAt(3).ColumnIndex);

            Assert.AreEqual("% Of Out. for ov. inc.", table.Columns.ElementAt(4).ColumnName);
            Assert.AreEqual(typeof(decimal?), table.Columns.ElementAt(4).ColumnType);
            Assert.AreEqual(4, table.Columns.ElementAt(4).ColumnIndex);

            Assert.AreEqual("Outcome", table.Columns.ElementAt(5).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(5).ColumnType);
            Assert.AreEqual(5, table.Columns.ElementAt(5).ColumnIndex);

            Assert.AreEqual("Monthly Outcome", table.Columns.ElementAt(6).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(6).ColumnType);
            Assert.AreEqual(6, table.Columns.ElementAt(6).ColumnIndex);

            Assert.AreEqual("Moneys Left", table.Columns.ElementAt(7).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(7).ColumnType);
            Assert.AreEqual(7, table.Columns.ElementAt(7).ColumnIndex);

            Assert.AreEqual("Ov. Moneys Left", table.Columns.ElementAt(8).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(8).ColumnType);
            Assert.AreEqual(8, table.Columns.ElementAt(8).ColumnIndex);

            Assert.AreEqual("Ov. Categ. Outcome", table.Columns.ElementAt(9).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(9).ColumnType);
            Assert.AreEqual(9, table.Columns.ElementAt(9).ColumnIndex);

            Assert.AreEqual("Saving Coeff", table.Columns.ElementAt(10).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(10).ColumnType);
            Assert.AreEqual(10, table.Columns.ElementAt(10).ColumnIndex);

            Assert.AreEqual(48, table.Count);
        }

        [TestMethod]
        public void RichStatsFakeBankFile2Test()
        {
            var query = @"
with BasicIndicators as (
	select 
		ExtractFromDate(DateTime, 'month') as 'Month', 
		ClusteredByContainsKey('./Files/Categories.txt', ChargeName) as 'Category', 
		SumIncome(ToDecimal(Amount)) as Income, 
		SumIncome(ToDecimal(Amount), 1) as 'MonthlyIncome',
		Round(PercentOf(Abs(SumOutcome(ToDecimal(Amount))), SumIncome(ToDecimal(Amount), 1)), 2) as 'PercOfOutForOvInc',	
		SumOutcome(ToDecimal(Amount)) as Outcome, 
		SumOutcome(ToDecimal(Amount), 1) as 'MonthlyOutcome',
		SumIncome(ToDecimal(Amount), 1) + SumOutcome(ToDecimal(Amount), 1) as 'MoneysLeft',
		SumIncome(ToDecimal(Amount), 2) + SumOutcome(ToDecimal(Amount), 2) as 'OvMoneysLeft'
	from #csv.file('./Files/FakeBankingTransactions.csv', ',', true, 0) as csv
	group by 
		ExtractFromDate(DateTime, 'month'), 
		ClusteredByContainsKey('./Files/Categories.txt', ChargeName)
), AggregatedCategories as (
	select Category, Sum(Outcome) as 'CategoryOutcome' from BasicIndicators group by Category
)
select
	BasicIndicators.Month,
	BasicIndicators.Category,
	BasicIndicators.Income,
	BasicIndicators.MonthlyIncome as 'Monthly Income',
	BasicIndicators.PercOfOutForOvInc as '% Of Out. for ov. inc.',
	BasicIndicators.Outcome,
	BasicIndicators.MonthlyOutcome as 'Monthly Outcome',
	BasicIndicators.MoneysLeft as 'Moneys Left',
	BasicIndicators.OvMoneysLeft as 'Ov. Moneys Left',
	AggregatedCategories.CategoryOutcome as 'Ov. Categ. Outcome',
    ((BasicIndicators.MonthlyIncome - BasicIndicators.MonthlyOutcome) / BasicIndicators.MonthlyIncome) as 'Saving Coeff'
from BasicIndicators inner join AggregatedCategories on BasicIndicators.Category = AggregatedCategories.Category";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(11, table.Columns.Count());

            Assert.AreEqual("BasicIndicators.Month", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnIndex);

            Assert.AreEqual("BasicIndicators.Category", table.Columns.ElementAt(1).ColumnName);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
            Assert.AreEqual(1, table.Columns.ElementAt(1).ColumnIndex);

            Assert.AreEqual("BasicIndicators.Income", table.Columns.ElementAt(2).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(2).ColumnType);
            Assert.AreEqual(2, table.Columns.ElementAt(2).ColumnIndex);

            Assert.AreEqual("Monthly Income", table.Columns.ElementAt(3).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(3).ColumnType);
            Assert.AreEqual(3, table.Columns.ElementAt(3).ColumnIndex);

            Assert.AreEqual("% Of Out. for ov. inc.", table.Columns.ElementAt(4).ColumnName);
            Assert.AreEqual(typeof(decimal?), table.Columns.ElementAt(4).ColumnType);
            Assert.AreEqual(4, table.Columns.ElementAt(4).ColumnIndex);

            Assert.AreEqual("BasicIndicators.Outcome", table.Columns.ElementAt(5).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(5).ColumnType);
            Assert.AreEqual(5, table.Columns.ElementAt(5).ColumnIndex);

            Assert.AreEqual("Monthly Outcome", table.Columns.ElementAt(6).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(6).ColumnType);
            Assert.AreEqual(6, table.Columns.ElementAt(6).ColumnIndex);

            Assert.AreEqual("Moneys Left", table.Columns.ElementAt(7).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(7).ColumnType);
            Assert.AreEqual(7, table.Columns.ElementAt(7).ColumnIndex);

            Assert.AreEqual("Ov. Moneys Left", table.Columns.ElementAt(8).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(8).ColumnType);
            Assert.AreEqual(8, table.Columns.ElementAt(8).ColumnIndex);

            Assert.AreEqual("Ov. Categ. Outcome", table.Columns.ElementAt(9).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(9).ColumnType);
            Assert.AreEqual(9, table.Columns.ElementAt(9).ColumnIndex);

            Assert.AreEqual("Saving Coeff", table.Columns.ElementAt(10).ColumnName);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(10).ColumnType);
            Assert.AreEqual(10, table.Columns.ElementAt(10).ColumnIndex);

            Assert.AreEqual(48, table.Count);
        }

        [TestMethod]
        public void CsvSource_CancelledLoadTest()
        {
            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();
            var source = new CsvSource("./Files/BankingTransactionsWithSkippedLines.csv", ",", true, 2, new RuntimeContext(tokenSource.Token, new ISchemaColumn[0]));

            var fired = source.Rows.Count();

            Assert.AreEqual(0, fired);
        }

        [TestMethod]
        public void CsvSource_AllTypesSupportedTest()
        {
            var tokenSource = new CancellationTokenSource();

            var columns = new List<ISchemaColumn>();

            columns.Add(new Column("boolColumn", typeof(bool?), 0));
            columns.Add(new Column("byteColumn", typeof(byte?), 1));
            columns.Add(new Column("charColumn", typeof(char?), 2));
            columns.Add(new Column("dateTimeColumn", typeof(DateTime?), 3));
            columns.Add(new Column("decimalColumn", typeof(decimal?), 4));
            columns.Add(new Column("doubleColumn", typeof(double?), 5));
            columns.Add(new Column("shortColumn", typeof(short?), 6));
            columns.Add(new Column("intColumn", typeof(int?), 7));
            columns.Add(new Column("longColumn", typeof(long?), 8));
            columns.Add(new Column("sbyteColumn", typeof(sbyte?), 9));
            columns.Add(new Column("singleColumn", typeof(float?), 10));
            columns.Add(new Column("stringColumn", typeof(string), 11));
            columns.Add(new Column("ushortColumn", typeof(ushort?), 12));
            columns.Add(new Column("uintColumn", typeof(uint?), 13));
            columns.Add(new Column("ulongColumn", typeof(ulong?), 14));

            var context = new RuntimeContext(tokenSource.Token, columns);

            var source = new CsvSource("./Files/AllTypes.csv", ",", true, 0, context);

            var rows = source.Rows;

            var row = rows.ElementAt(0);

            Assert.AreEqual(true, row[0]);
            Assert.AreEqual((byte)48, row[1]);
            Assert.AreEqual('c', row[2]);
            Assert.AreEqual(DateTime.Parse("12/12/2012"), row[3]);
            Assert.AreEqual(10.23m, row[4]);
            Assert.AreEqual(13.111d, row[5]);
            Assert.AreEqual((short)-15, row[6]);
            Assert.AreEqual(2147483647, row[7]);
            Assert.AreEqual(9223372036854775807, row[8]);
            Assert.AreEqual((sbyte)-3, row[9]);
            Assert.AreEqual(1.11f, row[10]);
            Assert.AreEqual("some text", row[11]);
            Assert.AreEqual((ushort)256, row[12]);
            Assert.AreEqual((uint)512, row[13]);
            Assert.AreEqual((ulong)1024, row[14]);
        }

        [TestMethod]
        public void DescSchemaTest()
        {
            var query = "desc #csv";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(5, table.Columns.Count());
            Assert.AreEqual(2, table.Count);
        }

        [TestMethod]
        public void DescMethodTest()
        {
            var query = "desc #csv.file";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(5, table.Columns.Count());
            Assert.AreEqual(1, table.Count);
        }

        [TestMethod]
        public void CsvSource_FullLoadTest()
        {
            var source = new CsvSource("./Files/BankingTransactionsWithSkippedLines.csv", ",", true, 2, RuntimeContext.Empty);

            var fired = source.Rows.Count();

            Assert.AreEqual(11, fired);
        }

        private CompiledQuery CreateAndRunVirtualMachine(string script)
        {
            return InstanceCreator.CompileForExecution(script, new CsvSchemaProvider());
        }

        static CsvTests()
        {
            new Environment().SetValue(Constants.NetStandardDllEnvironmentName, EnvironmentUtils.GetOrCreateEnvironmentVariable());
        }
    }
}