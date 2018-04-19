using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class BasicEvaluatorTests : TestBase
    {
        [TestMethod]
        public void LikeOperatorTest()
        {
            var query = "select Name from #A.Entities() where Name like '%AA%'";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("ABCAACBA"), new BasicEntity("AAeqwgQEW"), new BasicEntity("XXX"), new BasicEntity("dadsqqAA")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("ABCAACBA", table[0].Values[0]);
            Assert.AreEqual("AAeqwgQEW", table[1].Values[0]);
            Assert.AreEqual("dadsqqAA", table[2].Values[0]);
        }

        [TestMethod]
        public void ComplexWhere1Test()
        {
            var query = "select Population from #A.Entities() where Population > 0 and Population - 100 > -1.5d and Population - 100 < 1.5d";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 99),
                        new BasicEntity("KATOWICE", "POLAND", 101),
                        new BasicEntity("BERLIN", "GERMANY", 50)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Population", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual(99m, table[0].Values[0]);
            Assert.AreEqual(101m, table[1].Values[0]);
        }

        [TestMethod]
        public void NotLikeOperatorTest()
        {
            var query = "select Name from #A.Entities() where Name not like '%AA%'";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("ABCAACBA"), new BasicEntity("AAeqwgQEW"), new BasicEntity("XXX"), new BasicEntity("dadsqqAA")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("XXX", table[0].Values[0]);
        }

        [TestMethod]
        public void MultipleAndOperatorTest()
        {
            var query = "select Name from #A.Entities() where IndexOf(Name, 'A') = 0 and IndexOf(Name, 'B') = 1 and IndexOf(Name, 'C') = 2";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("A"), new BasicEntity("AB"), new BasicEntity("ABC"), new BasicEntity("ABCD")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("ABC", table[0].Values[0]);
            Assert.AreEqual("ABCD", table[1].Values[0]);
        }

        [TestMethod]
        public void MultipleOrOperatorTest()
        {
            var query = "select Name from #A.Entities() where Name = 'ABC' or Name = 'ABCD' or Name = 'A'";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("A"), new BasicEntity("AB"), new BasicEntity("ABC"), new BasicEntity("ABCD")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("A", table[0].Values[0]);
            Assert.AreEqual("ABC", table[1].Values[0]);
            Assert.AreEqual("ABCD", table[2].Values[0]);
        }

        [TestMethod]
        public void AddOperatorWithStringsTurnsIntoConcatTest()
        {
            var query = "select 'abc' + 'cda' from #A.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("ABCAACBA")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("'abc' + 'cda'", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("abccda", table[0].Values[0]);
        }

        [TestMethod]
        public void ContainsStringsTest()
        {
            var query = "select Name from #A.Entities() where Name contains ('ABC', 'CdA', 'CDA', 'DDABC')";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("ABC"), new BasicEntity("XXX"), new BasicEntity("CDA"), new BasicEntity("DDABC")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("ABC", table[0].Values[0]);
            Assert.AreEqual("CDA", table[1].Values[0]);
            Assert.AreEqual("DDABC", table[2].Values[0]);
        }

        [TestMethod]
        public void CanPassComplexArgumentToFunctionTest()
        {
            var query = "select NothingToDo(Self) from #A.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"){ Name = "ABBA", Country = "POLAND", City = "CRACOV", Money = 1.23m, Month = "JANUARY", Population = 250}}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("NothingToDo(Self)", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(BasicEntity), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(typeof(BasicEntity), table[0].Values[0].GetType());
        }

        [TestMethod]
        public void TableShouldComplexTypeTest()
        {
            var query = "select Self from #A.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"){ Name = "ABBA", Country = "POLAND", City = "CRACOV", Money = 1.23m, Month = "JANUARY", Population = 250}}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Self", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(BasicEntity), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(typeof(BasicEntity), table[0].Values[0].GetType());
        }

        [TestMethod]
        public void SimpleShowAllColumnsTest()
        {
            var entity = new BasicEntity("001")
            {
                Name = "ABBA",
                Country = "POLAND",
                City = "CRACOV",
                Money = 1.23m,
                Month = "JANUARY",
                Population = 250,
                Time = DateTime.MaxValue,
                Id = 5
            };
            var query = "select 1, *, Name as Name2, ToString(Self) as SelfString from #A.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {entity}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();
            Assert.AreEqual("1", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(Int64), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual("Name", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);

            Assert.AreEqual("City", table.Columns.ElementAt(2).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);

            Assert.AreEqual("Country", table.Columns.ElementAt(3).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(3).ColumnType);

            Assert.AreEqual("Population", table.Columns.ElementAt(4).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(4).ColumnType);

            Assert.AreEqual("Self", table.Columns.ElementAt(5).Name);
            Assert.AreEqual(typeof(BasicEntity), table.Columns.ElementAt(5).ColumnType);

            Assert.AreEqual("Money", table.Columns.ElementAt(6).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(6).ColumnType);

            Assert.AreEqual("Month", table.Columns.ElementAt(7).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(7).ColumnType);

            Assert.AreEqual("Time", table.Columns.ElementAt(8).Name);
            Assert.AreEqual(typeof(DateTime), table.Columns.ElementAt(8).ColumnType);

            Assert.AreEqual("Id", table.Columns.ElementAt(9).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(9).ColumnType);

            Assert.AreEqual("Name2", table.Columns.ElementAt(10).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(10).ColumnType);

            Assert.AreEqual("SelfString", table.Columns.ElementAt(11).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(11).ColumnType);

            Assert.AreEqual(1, table.Count);

            Assert.AreEqual(Convert.ToInt64(1), table[0].Values[0]);
            Assert.AreEqual("ABBA", table[0].Values[1]);
            Assert.AreEqual("CRACOV", table[0].Values[2]);
            Assert.AreEqual("POLAND", table[0].Values[3]);
            Assert.AreEqual(250m, table[0].Values[4]);
            Assert.AreEqual(entity, table[0].Values[5]);
            Assert.AreEqual(1.23m, table[0].Values[6]);
            Assert.AreEqual("JANUARY", table[0].Values[7]);
            Assert.AreEqual(DateTime.MaxValue, table[0].Values[8]);
            Assert.AreEqual(5, table[0].Values[9]);
            Assert.AreEqual("ABBA", table[0].Values[10]);
            Assert.AreEqual("TEST STRING", table[0].Values[11]);
        }

        [TestMethod]
        public void SimpleAccessObjectTest()
        {
            var query = @"select Self.Array[2] from #A.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual("Self.Array[2]", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(Int32), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual(2, table[0].Values[0]);
            Assert.AreEqual(2, table[1].Values[0]);
        }

        [TestMethod]
        public void SimpleAccessObjectIncrementTest()
        {
            var query = @"select Inc(Self.Array[2]) from #A.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual("Inc(Self.Array[2])", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(Int64), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual(Convert.ToInt64(3), table[0].Values[0]);
            Assert.AreEqual(Convert.ToInt64(3), table[1].Values[0]);
        }

        [TestMethod]
        public void WhereWithOrTest()
        {
            var query = @"select Name from #A.Entities() where Name = '001' or Name = '005'";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"),  new BasicEntity("002"), new BasicEntity("005")}},
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("005", table[1].Values[0]);
        }

        [TestMethod]
        public void SimpleQueryTest()
        {
            var query = @"select Name from #A.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
        }

        [TestMethod]
        public void SimpleSkipTest()
        {
            var query = @"select Name from #A.Entities() skip 2";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("005"), new BasicEntity("006")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(4, table.Count);
            Assert.AreEqual("003", table[0].Values[0]);
            Assert.AreEqual("004", table[1].Values[0]);
            Assert.AreEqual("005", table[2].Values[0]);
            Assert.AreEqual("006", table[3].Values[0]);
        }

        [TestMethod]
        public void SimpleTakeTest()
        {
            var query = @"select Name from #A.Entities() take 2";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("005"), new BasicEntity("006")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
        }

        [TestMethod]
        public void SimpleSkipTakeTest()
        {
            var query = @"select Name from #A.Entities() skip 1 take 2";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("005"), new BasicEntity("006")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("002", table[0].Values[0]);
            Assert.AreEqual("003", table[1].Values[0]);
        }

        [TestMethod]
        public void SimpleSkipAboveTableAmountTest()
        {
            var query = @"select Name from #A.Entities() skip 100";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("005"), new BasicEntity("006")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(0, table.Count);
        }

        [TestMethod]
        public void SimpleTakeAboveTableAmountTest()
        {
            var query = @"select Name from #A.Entities() take 100";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("005"), new BasicEntity("006")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(6, table.Count);
            Assert.AreEqual("001", table[0].Values[0]);
            Assert.AreEqual("002", table[1].Values[0]);
            Assert.AreEqual("003", table[2].Values[0]);
            Assert.AreEqual("004", table[3].Values[0]);
            Assert.AreEqual("005", table[4].Values[0]);
            Assert.AreEqual("006", table[5].Values[0]);
        }

        [TestMethod]
        public void SimpleSkipTakeAboveTableAmountTest()
        {
            var query = @"select Name from #A.Entities() skip 100 take 100";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("005"), new BasicEntity("006")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(0, table.Count);
        }



        [TestMethod]
        public void ColumnNamesSimpleTest()
        {
            var query = @"select Name, GetOne(), GetOne() as TestColumn, GetTwo(4d, 'test') from #A.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new BasicEntity[] { }}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(4, table.Columns.Count());
            Assert.AreEqual("Name", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual("GetOne()", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);

            Assert.AreEqual("TestColumn", table.Columns.ElementAt(2).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(2).ColumnType);

            Assert.AreEqual("GetTwo(4, 'test')", table.Columns.ElementAt(3).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(3).ColumnType);
        }

        [TestMethod]
        public void CallMethodWithTwoParametersTest()
        {
            var query = @"select Concat(Country, ToString(Population)) from #A.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("ABBA", 200)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Concat(Country, ToString(Population))", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("ABBA200", table[0].Values[0]);
        }

        [TestMethod]
        public void ColumnTypeDateTimeTest()
        {
            var query = "select Time from #A.entities()";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity(DateTime.MinValue)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Time", table.Columns.ElementAt(0).Name);

            Assert.AreEqual(1, table.Count());
            Assert.AreEqual(DateTime.MinValue, table[0].Values[0]);
        }

        [TestMethod]
        public void SimpleRowNumberStatTest()
        {
            var query = @"select RowNumber() from #A.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("005"), new BasicEntity("006")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(6, table.Count);
            Assert.AreEqual(1, table[0].Values[0]);
            Assert.AreEqual(2, table[1].Values[0]);
            Assert.AreEqual(3, table[2].Values[0]);
            Assert.AreEqual(4, table[3].Values[0]);
            Assert.AreEqual(5, table[4].Values[0]);
            Assert.AreEqual(6, table[5].Values[0]);
        }

        [TestMethod]
        public void SelectDecimalWithoutMarkingNumberExplicitlyTest()
        {
            var query = "select 1.0, -1.0 from #A.entities()";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("xX"),
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("1.0", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual("-1.0", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(decimal), table.Columns.ElementAt(1).ColumnType);

            Assert.AreEqual(1, table.Count());
            Assert.AreEqual(1.0m, table[0].Values[0]);
            Assert.AreEqual(-1.0m, table[0].Values[1]);
        }

        [TestMethod]
        public void DescPlugin()
        {
            var query = "desc #A.entities()";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("xX"),
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();

            Assert.AreEqual(3, table.Columns.Count());
            Assert.AreEqual(9, table.Count);

            Assert.AreEqual("Name", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual("Index", table.Columns.ElementAt(1).Name);
            Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);

            Assert.AreEqual("Type", table.Columns.ElementAt(2).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);

            Assert.AreEqual("Name", table[0][0]);
            Assert.AreEqual(10, table[0][1]);
            Assert.AreEqual("String", table[0][2]);

            Assert.AreEqual("City", table[1][0]);
            Assert.AreEqual(11, table[1][1]);
            Assert.AreEqual("String", table[1][2]);

            Assert.AreEqual("Country", table[2][0]);
            Assert.AreEqual(12, table[2][1]);
            Assert.AreEqual("String", table[2][2]);

            Assert.AreEqual("Population", table[3][0]);
            Assert.AreEqual(13, table[3][1]);
            Assert.AreEqual("Decimal", table[3][2]);

            Assert.AreEqual("Self", table[4][0]);
            Assert.AreEqual(14, table[4][1]);
            Assert.AreEqual("BasicEntity", table[4][2]);

            Assert.AreEqual("Money", table[5][0]);
            Assert.AreEqual(15, table[5][1]);
            Assert.AreEqual("Decimal", table[5][2]);

            Assert.AreEqual("Month", table[6][0]);
            Assert.AreEqual(16, table[6][1]);
            Assert.AreEqual("String", table[6][2]);

            Assert.AreEqual("Time", table[7][0]);
            Assert.AreEqual(17, table[7][1]);
            Assert.AreEqual("DateTime", table[7][2]);

            Assert.AreEqual("Id", table[8][0]);
            Assert.AreEqual(18, table[8][1]);
            Assert.AreEqual("Int32", table[8][2]);
        }
        
        [TestMethod]
        public void SimpleJoinTest()
        {
            var query = "select a.Id from #A.x1() a inner join #B.x2() b on a.Id = b.Id";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("xX"),
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();
        }

        [Ignore]
        [TestMethod]
        public void SimpleLeftJoinTest()
        {
            var query = "select a.Id from #A.x1() a left outer join #B.x2() b on a.Id = b.Id";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("xX"),
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Execute();
        }
    }
}