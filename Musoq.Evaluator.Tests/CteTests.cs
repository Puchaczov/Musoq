using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class CteTests : BasicEntityTestBase
    {
        [TestMethod]
        public void SimpleCteTest()
        {
            var query = "with p as (select City, Country from #A.entities()) select Country, City from p";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", [
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                        new BasicEntity("KATOWICE", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350)
                    ]
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("Country", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual("City", table.Columns.ElementAt(1).ColumnName);

            Assert.AreEqual(5, table.Count());

            Assert.AreEqual("POLAND", table[0].Values[0]);
            Assert.AreEqual("WARSAW", table[0].Values[1]);

            Assert.AreEqual("POLAND", table[1].Values[0]);
            Assert.AreEqual("CZESTOCHOWA", table[1].Values[1]);

            Assert.AreEqual("POLAND", table[2].Values[0]);
            Assert.AreEqual("KATOWICE", table[2].Values[1]);

            Assert.AreEqual("GERMANY", table[3].Values[0]);
            Assert.AreEqual("BERLIN", table[3].Values[1]);

            Assert.AreEqual("GERMANY", table[4].Values[0]);
            Assert.AreEqual("MUNICH", table[4].Values[1]);
        }

        [TestMethod]
        public void SimpleCteWithStarTest()
        {
            var query = "with p as (select City, Country from #A.entities()) select * from p";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                        new BasicEntity("KATOWICE", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

            Assert.AreEqual(5, table.Count());

            Assert.AreEqual("WARSAW", table[0].Values[0]);
            Assert.AreEqual("POLAND", table[0].Values[1]);

            Assert.AreEqual("CZESTOCHOWA", table[1].Values[0]);
            Assert.AreEqual("POLAND", table[1].Values[1]);

            Assert.AreEqual("KATOWICE", table[2].Values[0]);
            Assert.AreEqual("POLAND", table[2].Values[1]);

            Assert.AreEqual("BERLIN", table[3].Values[0]);
            Assert.AreEqual("GERMANY", table[3].Values[1]);

            Assert.AreEqual("MUNICH", table[4].Values[0]);
            Assert.AreEqual("GERMANY", table[4].Values[1]);
        }

        [TestMethod]
        public void SimpleCteWithGroupingTest()
        {
            var query =
                "with p as (select Country, Sum(Population) from #A.entities() group by Country) select * from p";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                        new BasicEntity("KATOWICE", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("Country", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual("Sum(Population)", table.Columns.ElementAt(1).ColumnName);

            Assert.AreEqual(2, table.Count());

            Assert.AreEqual("POLAND", table[0].Values[0]);
            Assert.AreEqual(1150m, table[0].Values[1]);

            Assert.AreEqual("GERMANY", table[1].Values[0]);
            Assert.AreEqual(600m, table[1].Values[1]);
        }


        [TestMethod]
        public void SimpleCteWithGrouping2Test()
        {
            var query =
                "with p as (select Population, Country from #A.entities()) select Country, Sum(Population) from p group by Country";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                        new BasicEntity("KATOWICE", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("Country", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual("Sum(Population)", table.Columns.ElementAt(1).ColumnName);

            Assert.AreEqual(2, table.Count());

            Assert.AreEqual("POLAND", table[0].Values[0]);
            Assert.AreEqual(1150m, table[0].Values[1]);

            Assert.AreEqual("GERMANY", table[1].Values[0]);
            Assert.AreEqual(600m, table[1].Values[1]);
        }

        [TestMethod]
        public void SimpleCteWithUnionTest()
        {
            var query =
                "with p as (select City, Country from #A.entities() union (Country, City) select City, Country from #B.entities()) select City, Country from p";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("KATOWICE", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

            Assert.AreEqual(5, table.Count());

            Assert.AreEqual("WARSAW", table[0].Values[0]);
            Assert.AreEqual("POLAND", table[0].Values[1]);

            Assert.AreEqual("CZESTOCHOWA", table[1].Values[0]);
            Assert.AreEqual("POLAND", table[1].Values[1]);

            Assert.AreEqual("KATOWICE", table[2].Values[0]);
            Assert.AreEqual("POLAND", table[2].Values[1]);

            Assert.AreEqual("BERLIN", table[3].Values[0]);
            Assert.AreEqual("GERMANY", table[3].Values[1]);

            Assert.AreEqual("MUNICH", table[4].Values[0]);
            Assert.AreEqual("GERMANY", table[4].Values[1]);
        }

        [TestMethod]
        public void SimpleCteWithUnionAllTest()
        {
            var query =
                "with p as (select City, Country from #A.entities() union all (Country) select City, Country from #B.entities()) select City, Country from p";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("KATOWICE", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

            Assert.AreEqual(5, table.Count());

            Assert.AreEqual("WARSAW", table[0].Values[0]);
            Assert.AreEqual("POLAND", table[0].Values[1]);

            Assert.AreEqual("CZESTOCHOWA", table[1].Values[0]);
            Assert.AreEqual("POLAND", table[1].Values[1]);

            Assert.AreEqual("KATOWICE", table[2].Values[0]);
            Assert.AreEqual("POLAND", table[2].Values[1]);

            Assert.AreEqual("BERLIN", table[3].Values[0]);
            Assert.AreEqual("GERMANY", table[3].Values[1]);

            Assert.AreEqual("MUNICH", table[4].Values[0]);
            Assert.AreEqual("GERMANY", table[4].Values[1]);
        }

        [TestMethod]
        public void SimpleCteWithExceptTest()
        {
            var query =
                "with p as (select City, Country from #A.entities() except (Country) select City, Country from #B.entities()) select City, Country from p";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("HELSINKI", "FINLAND", 500),
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("KATOWICE", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

            Assert.AreEqual(1, table.Count());

            Assert.AreEqual("HELSINKI", table[0].Values[0]);
            Assert.AreEqual("FINLAND", table[0].Values[1]);
        }

        [TestMethod]
        public void SimpleCteWithIntersectTest()
        {
            var query =
                "with p as (select City, Country from #A.entities() intersect (Country, City) select City, Country from #B.entities()) select City, Country from p";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("HELSINKI", "FINLAND", 500),
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350),
                        new BasicEntity("HELSINKI", "FINLAND", 500)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

            Assert.AreEqual(2, table.Count());

            Assert.AreEqual("HELSINKI", table[0].Values[0]);
            Assert.AreEqual("FINLAND", table[0].Values[1]);

            Assert.AreEqual("WARSAW", table[1].Values[0]);
            Assert.AreEqual("POLAND", table[1].Values[1]);
        }

        [TestMethod]
        public void CteWithSetOperatorTest()
        {
            var query = @"
with p as (
    select City, Country from #A.entities() intersect (Country, City) 
    select City, Country from #B.entities()
) 
select City, Country from p";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("HELSINKI", "FINLAND", 500),
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350),
                        new BasicEntity("HELSINKI", "FINLAND", 500)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

            Assert.AreEqual(2, table.Count());

            Assert.AreEqual("HELSINKI", table[0].Values[0]);
            Assert.AreEqual("FINLAND", table[0].Values[1]);

            Assert.AreEqual("WARSAW", table[1].Values[0]);
            Assert.AreEqual("POLAND", table[1].Values[1]);
        }

        [TestMethod]
        public void CteWithTwoOuterExpressionTest()
        {
            var query = @"
with p as (
    select City, Country from #A.entities()
) 
select City, Country from p union (City, Country)
select City, Country from #B.entities()";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("HELSINKI", "FINLAND", 500),
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350),
                        new BasicEntity("HELSINKI", "FINLAND", 500)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

            Assert.AreEqual(5, table.Count());

            Assert.AreEqual("HELSINKI", table[0].Values[0]);
            Assert.AreEqual("FINLAND", table[0].Values[1]);

            Assert.AreEqual("WARSAW", table[1].Values[0]);
            Assert.AreEqual("POLAND", table[1].Values[1]);

            Assert.AreEqual("CZESTOCHOWA", table[2].Values[0]);
            Assert.AreEqual("POLAND", table[2].Values[1]);

            Assert.AreEqual("BERLIN", table[3].Values[0]);
            Assert.AreEqual("GERMANY", table[3].Values[1]);

            Assert.AreEqual("MUNICH", table[4].Values[0]);
            Assert.AreEqual("GERMANY", table[4].Values[1]);
        }

        [TestMethod]
        public void SimpleCteWithMultipleOuterExpressionsTest()
        {
            var query = @"
with p as (
    select City, Country from #A.entities() intersect (Country, City) 
    select City, Country from #B.entities()
) select City, Country from p where Country = 'FINLAND' union (Country, City)
  select City, Country from p where Country = 'POLAND'";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("HELSINKI", "FINLAND", 500),
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350),
                        new BasicEntity("TOKYO", "JAPAN", 500),
                        new BasicEntity("HELSINKI", "FINLAND", 500)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

            Assert.AreEqual(2, table.Count());

            Assert.AreEqual("HELSINKI", table[0].Values[0]);
            Assert.AreEqual("FINLAND", table[0].Values[1]);

            Assert.AreEqual("WARSAW", table[1].Values[0]);
            Assert.AreEqual("POLAND", table[1].Values[1]);
        }

        [TestMethod]
        public void CteWithSetInInnerOuterExpressionTest()
        {
            var query = @"
with p as (
    select City, Country from #A.entities() intersect (Country, City) 
    select City, Country from #B.entities()
) 
select City, Country from p union (City, Country)
select City, Country from #C.Entities()";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("HELSINKI", "FINLAND", 500),
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350),
                        new BasicEntity("HELSINKI", "FINLAND", 500)
                    }
                },
                {
                    "#C",
                    new[]
                    {
                        new BasicEntity("NEW YORK", "USA", 250)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

            Assert.AreEqual(3, table.Count());

            Assert.AreEqual("HELSINKI", table[0].Values[0]);
            Assert.AreEqual("FINLAND", table[0].Values[1]);

            Assert.AreEqual("WARSAW", table[1].Values[0]);
            Assert.AreEqual("POLAND", table[1].Values[1]);

            Assert.AreEqual("NEW YORK", table[2].Values[0]);
            Assert.AreEqual("USA", table[2].Values[1]);
        }

        [TestMethod]
        public void MultipleCteExpressionsTest()
        {
            const string query = @"
with p as (
    select City, Country from #A.entities()
), c as (
    select City, Country from #B.entities()
), d as (
    select City, Country from p where City = 'HELSINKI'
), f as (
    select City, Country from #B.entities() where City = 'WARSAW'
)
select City, Country from p union (City, Country)
select City, Country from c union (City, Country)
select City, Country from d union (City, Country)
select City, Country from f";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("HELSINKI", "FINLAND", 500),
                        new BasicEntity("WARSAW", "POLAND", 500),
                        new BasicEntity("CZESTOCHOWA", "POLAND", 400)
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("WARSAW", "POLAND", 250),
                        new BasicEntity("BERLIN", "GERMANY", 250),
                        new BasicEntity("MUNICH", "GERMANY", 350),
                        new BasicEntity("HELSINKI", "FINLAND", 500)
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Columns.Count());
            Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);

            Assert.AreEqual(5, table.Count());

            Assert.AreEqual("HELSINKI", table[0].Values[0]);
            Assert.AreEqual("FINLAND", table[0].Values[1]);

            Assert.AreEqual("WARSAW", table[1].Values[0]);
            Assert.AreEqual("POLAND", table[1].Values[1]);

            Assert.AreEqual("CZESTOCHOWA", table[2].Values[0]);
            Assert.AreEqual("POLAND", table[2].Values[1]);

            Assert.AreEqual("BERLIN", table[3].Values[0]);
            Assert.AreEqual("GERMANY", table[3].Values[1]);

            Assert.AreEqual("MUNICH", table[4].Values[0]);
            Assert.AreEqual("GERMANY", table[4].Values[1]);
        }

        [TestMethod]
        public void WhenMixedQueriesGroupingAreUsed_Variant1_ShouldPass()
        {
            var query = @"
with first as (
    select 1 as Test from #A.entities()
), second as (
    select 2 as Test from #B.entities() group by 'fake'
)
select c.GetBytes(c.Name) from first a 
    inner join #A.entities() c on 1 = 1 
    inner join second b on 1 = 1";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("First"),
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("Second"),
                    }
                }
            };
            
            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();
            
            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("First", Encoding.UTF8.GetString((byte[])table[0].Values[0]));
        }

        [TestMethod]
        public void WhenMixedQueriesGroupingAreUsed_Variant2_ShouldPass()
        {
            var query = @"
with first as (
    select Name from #A.entities()
), second as (
    select Name from #B.entities() group by Name
)
select 
    c.GetBytes(c.Name) 
from first a 
    inner join #A.entities() c on 1 = 1 
    inner join second b on 1 = 1";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("First"),
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("Second"),
                    }
                }
            };
            
            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();
            
            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("First", Encoding.UTF8.GetString((byte[])table[0].Values[0]));
        }

        [TestMethod]
        public void WhenMixedQueriesGroupingAreUsed_Variant3_ShouldPass()
        {
            var query = @"
with first as (
    select Name from #A.entities()
), second as (
    select Name from #B.entities() group by Name
)
select
    a.Name,
    b.Name,
    c.GetBytes(c.Name) 
from first a 
    inner join #A.entities() c on 1 = 1 
    inner join second b on 1 = 1";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("First"),
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("Second"),
                    }
                }
            };
            
            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();
            
            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("First", (string)table[0].Values[0]);
            Assert.AreEqual("Second", (string)table[0].Values[1]);
            Assert.AreEqual("First", Encoding.UTF8.GetString((byte[])table[0].Values[2]));
        }

        [TestMethod]
        public void WhenMixedQueriesGroupingAreUsed_Variant4_ShouldPass()
        {
            var query = @"
with first as (
    select a.Name as Name from #A.entities() a group by a.Name having Count(a.Name) > 0
), second as (
    select b.Name() as Name from #B.entities() b inner join first a on 1 = 1
), third as (
    select b.Name as Name from second b group by b.Name having Count(b.Name) > 0
), fourth as (
    select
        c.Name() as Name
    from second b
        inner join #A.entities() c on 1 = 1
        inner join third d on 1 = 1
)
select
    c.Name as Name
from fourth c";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("First"),
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("Second"),
                    }
                },
                {
                    "#C",
                    new[]
                    {
                        new BasicEntity("Third"),
                    }
                }
            };
            
            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();
            
            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("First", (string)table[0].Values[0]);
        }
    }
}