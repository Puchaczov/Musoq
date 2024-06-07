using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class OrderByTests : BasicEntityTestBase
    {
        [TestMethod]
        public void WhenOrderByColumn_ShouldSucceed()
        {
            var query = @"select City from #A.Entities() order by Money";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(-200))
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("cracow", table[0].Values[0]);
            Assert.AreEqual("katowice", table[1].Values[0]);
            Assert.AreEqual("czestochowa", table[2].Values[0]);
        }

        [TestMethod]
        public void WhenOrderByDescColumn_ShouldSucceed()
        {
            var query = @"select City from #A.Entities() order by Money desc";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(-200))
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("czestochowa", table[0].Values[0]);
            Assert.AreEqual("katowice", table[1].Values[0]);
            Assert.AreEqual("cracow", table[2].Values[0]);
        }
        
        [TestMethod]
        public void WhenOrderByMultipleColumnFirstDesc_ShouldSucceed()
        {
            var query = @"select City from #A.Entities() order by Money desc, Name";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(-200))
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("czestochowa", table[0].Values[0]);
            Assert.AreEqual("katowice", table[1].Values[0]);
            Assert.AreEqual("cracow", table[2].Values[0]);
        }
        
        [TestMethod]
        public void WhenOrderByMultipleColumns_ShoulSucceed()
        {
            var query = @"select City from #A.Entities() order by Money, Name";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(-200))
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("cracow", table[0].Values[0]);
            Assert.AreEqual("katowice", table[1].Values[0]);
            Assert.AreEqual("czestochowa", table[2].Values[0]);
        }
        
        [TestMethod]
        public void WhenOrderByMultipleColumnsBothDesc_ShouldSucceed()
        {
            var query = @"select City from #A.Entities() order by Money desc, Name desc";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(-200))
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("czestochowa", table[0].Values[0]);
            Assert.AreEqual("katowice", table[1].Values[0]);
            Assert.AreEqual("cracow", table[2].Values[0]);
        }
        
        [TestMethod]
        public void WhenOrderByMultipleColumnsSecondColumnDesc_ShouldSucceed()
        {
            var query = @"select City + '-' + ToString(Money) from #A.Entities() order by City, Money desc";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(10))
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(4, table.Count);
            Assert.AreEqual("cracow-10", table[0].Values[0]);
            Assert.AreEqual("czestochowa-400", table[1].Values[0]);
            Assert.AreEqual("katowice-300", table[2].Values[0]);
            Assert.AreEqual("katowice-100", table[3].Values[0]);
        }
        
        [TestMethod]
        public void WhenOrderByAfterGroupBy_ShouldSuccess()
        {
            var query = @"select City from #A.Entities() group by City order by City";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(10))
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("cracow", table[0].Values[0]);
            Assert.AreEqual("czestochowa", table[1].Values[0]);
            Assert.AreEqual("katowice", table[2].Values[0]);
        }
        
        [TestMethod]
        public void WhenOrderByWithDescAfterGroupBy_ShouldSucceed()
        {
            var query = @"select City from #A.Entities() group by City order by City desc";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(10))
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("katowice", table[0].Values[0]);
            Assert.AreEqual("czestochowa", table[1].Values[0]);
            Assert.AreEqual("cracow", table[2].Values[0]);
        }
        
        [TestMethod]
        public void WhenOrderByWithGroupByMultipleColumnAndFirstDesc_ShouldSucceed()
        {
            var query = @"select City, Money from #A.Entities() group by City, Money order by City desc, Money";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(10))
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(4, table.Count);
            Assert.AreEqual("katowice", table[0].Values[0]);
            Assert.AreEqual(100m, table[0].Values[1]);
            Assert.AreEqual("katowice", table[1].Values[0]);
            Assert.AreEqual(300m, table[1].Values[1]);
            Assert.AreEqual("czestochowa", table[2].Values[0]);
            Assert.AreEqual(400m, table[2].Values[1]);
            Assert.AreEqual("cracow", table[3].Values[0]);
            Assert.AreEqual(10m, table[3].Values[1]);
        }
        
        [TestMethod]
        public void WhenOrderByAfterGroupByMultipleColumnBothDesc_ShouldSucceed()
        {
            var query = @"select City, Money from #A.Entities() group by City, Money order by City desc, Money desc";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(10))
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(4, table.Count);
            Assert.AreEqual("katowice", table[0].Values[0]);
            Assert.AreEqual(300m, table[0].Values[1]);
            Assert.AreEqual("katowice", table[1].Values[0]);
            Assert.AreEqual(100m, table[1].Values[1]);
            Assert.AreEqual("czestochowa", table[2].Values[0]);
            Assert.AreEqual(400m, table[2].Values[1]);
            Assert.AreEqual("cracow", table[3].Values[0]);
            Assert.AreEqual(10m, table[3].Values[1]);
        }
        
        [TestMethod]
        public void WhenOrderByAfterGroupByHaving_ShouldSucceed()
        {
            var query = @"select City, Sum(Money) from #A.Entities() group by City having Sum(Money) >= 400 order by City";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(10))
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("czestochowa", table[0].Values[0]);
            Assert.AreEqual(400m, table[0].Values[1]);
            Assert.AreEqual("katowice", table[1].Values[0]);
            Assert.AreEqual(400m, table[1].Values[1]);
        }
        
        [TestMethod]
        public void WhenOrderByDescAfterGroupByHaving_ShouldSucceed()
        {
            var query = @"select City, Sum(Money) from #A.Entities() group by City having Sum(Money) >= 400 order by City desc";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(10))
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("katowice", table[0].Values[0]);
            Assert.AreEqual(400m, table[0].Values[1]);
            Assert.AreEqual("czestochowa", table[1].Values[0]);
            Assert.AreEqual(400m, table[1].Values[1]);
        }
        
        [TestMethod]
        public void WhenOrderByClauseWithOperation_ShouldSucceed()
        {
            const string query = @"select Money from #A.Entities() order by Money * -1";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                        new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                    }
                }
            };
            
            var vm = CreateAndRunVirtualMachine(query, sources);
            
            var table = vm.Run();
            
            Assert.AreEqual(5, table.Count);
            Assert.AreEqual(400m, table[0].Values[0]);
            Assert.AreEqual(300m, table[1].Values[0]);
            Assert.AreEqual(100m, table[2].Values[0]);
            Assert.AreEqual(10m, table[3].Values[0]);
            Assert.AreEqual(-10m, table[4].Values[0]);
        }
        
        [TestMethod]
        public void WhenOrderByClauseWithOperationDesc_ShouldSucceed()
        {
            var query = @"select Money from #A.Entities() order by Money * -1 desc";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                        new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                    }
                }
            };
            
            var vm = CreateAndRunVirtualMachine(query, sources);
            
            var table = vm.Run();
            
            Assert.AreEqual(5, table.Count);
            Assert.AreEqual(-10m, table[0].Values[0]);
            Assert.AreEqual(10m, table[1].Values[0]);
            Assert.AreEqual(100m, table[2].Values[0]);
            Assert.AreEqual(300m, table[3].Values[0]);
            Assert.AreEqual(400m, table[4].Values[0]);
        }

        [TestMethod]
        public void WhenOrderByWithinCteExpression_ShouldSucceed()
        {
            const string query = @"with cte as ( select City, Money from #A.Entities() order by Money ) select City from cte";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                        new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                    }
                }
            };
            
            var vm = CreateAndRunVirtualMachine(query, sources);
            
            var table = vm.Run();
            
            Assert.AreEqual(5, table.Count);
            Assert.AreEqual("glasgow", table[0].Values[0]);
            Assert.AreEqual("cracow", table[1].Values[0]);
            Assert.AreEqual("katowice", table[2].Values[0]);
            Assert.AreEqual("katowice", table[3].Values[0]);
            Assert.AreEqual("czestochowa", table[4].Values[0]);
        }
        
        [TestMethod]
        public void WhenOrderByDescWithinCteExpression_ShouldSucceed()
        {
            const string query = @"with cte as ( select City, Money from #A.Entities() order by Money desc ) select City from cte";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                        new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                    }
                }
            };
            
            var vm = CreateAndRunVirtualMachine(query, sources);
            
            var table = vm.Run();
            
            Assert.AreEqual(5, table.Count);
            Assert.AreEqual("czestochowa", table[0].Values[0]);
            Assert.AreEqual("katowice", table[1].Values[0]);
            Assert.AreEqual("katowice", table[2].Values[0]);
            Assert.AreEqual("cracow", table[3].Values[0]);
            Assert.AreEqual("glasgow", table[4].Values[0]);
        }
        
        [TestMethod]
        public void WhenOrderByWithMultipleColumnsFirstDescWithinCteExpression_ShouldSucceed()
        {
            const string query = @"with cte as ( select City, Money from #A.Entities() order by Money desc, City ) select City from cte";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                        new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                    }
                }
            };
            
            var vm = CreateAndRunVirtualMachine(query, sources);
            
            var table = vm.Run();
            
            Assert.AreEqual(5, table.Count);
            Assert.AreEqual("czestochowa", table[0].Values[0]);
            Assert.AreEqual("katowice", table[1].Values[0]);
            Assert.AreEqual("katowice", table[2].Values[0]);
            Assert.AreEqual("cracow", table[3].Values[0]);
            Assert.AreEqual("glasgow", table[4].Values[0]);
        }
        
        [TestMethod]
        public void WhenOrderByWithMultipleColumnsBothDescWithinCteExpression_ShouldSucceed()
        {
            const string query = @"with cte as ( select City, Money from #A.Entities() order by Money desc, City desc ) select City from cte";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                        new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                    }
                }
            };
            
            var vm = CreateAndRunVirtualMachine(query, sources);
            
            var table = vm.Run();
            
            Assert.AreEqual(5, table.Count);
            Assert.AreEqual("czestochowa", table[0].Values[0]);
            Assert.AreEqual("katowice", table[1].Values[0]);
            Assert.AreEqual("katowice", table[2].Values[0]);
            Assert.AreEqual("cracow", table[3].Values[0]);
            Assert.AreEqual("glasgow", table[4].Values[0]);
        }
        
        [TestMethod]
        public void WhenOrderByWithMultipleColumnsBothDescWithinCteExpression_BothRetrieved_ShouldSucceed()
        {
            const string query = @"with cte as ( select City, Money from #A.Entities() order by Money desc, City desc ) select City, Money from cte";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                        new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                    }
                }
            };
            
            var vm = CreateAndRunVirtualMachine(query, sources);
            
            var table = vm.Run();
            
            Assert.AreEqual(5, table.Count);
            Assert.AreEqual("czestochowa", table[0].Values[0]);
            Assert.AreEqual(400m, table[0].Values[1]);
            Assert.AreEqual("katowice", table[1].Values[0]);
            Assert.AreEqual(300m, table[1].Values[1]);
            Assert.AreEqual("katowice", table[2].Values[0]);
            Assert.AreEqual(100m, table[2].Values[1]);
            Assert.AreEqual("cracow", table[3].Values[0]);
            Assert.AreEqual(10m, table[3].Values[1]);
            Assert.AreEqual("glasgow", table[4].Values[0]);
            Assert.AreEqual(-10m, table[4].Values[1]);
        }

        [TestMethod]
        public void WhenOrderByCaseWhenExpression_ShouldSucceed()
        {
            var query = @"select City from #A.Entities() order by case when Money > 0 then Money else 0d end";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                        new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                    }
                }
            };
            
            var vm = CreateAndRunVirtualMachine(query, sources);
            
            var table = vm.Run();
            
            Assert.AreEqual(5, table.Count);
            Assert.AreEqual("glasgow", table[0].Values[0]);
            Assert.AreEqual("cracow", table[1].Values[0]);
            Assert.AreEqual("katowice", table[2].Values[0]);
            Assert.AreEqual("katowice", table[3].Values[0]);
            Assert.AreEqual("czestochowa", table[4].Values[0]);
        }
        
        [TestMethod]
        public void WhenOrderByCaseWhenDescExpression_ShouldSucceed()
        {
            var query = @"select City from #A.Entities() order by case when Money > 0 then Money else 0d end desc";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                        new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                    }
                }
            };
            
            var vm = CreateAndRunVirtualMachine(query, sources);
            
            var table = vm.Run();
            
            Assert.AreEqual(5, table.Count);
            Assert.AreEqual("czestochowa", table[0].Values[0]);
            Assert.AreEqual("katowice", table[1].Values[0]);
            Assert.AreEqual("katowice", table[2].Values[0]);
            Assert.AreEqual("cracow", table[3].Values[0]);
            Assert.AreEqual("glasgow", table[4].Values[0]);
        }
        
        [TestMethod]
        public void WhenOrderByMultipleColumnsFirstOneIsCaseWhenExpression_ShouldSucceed()
        {
            var query = @"select City from #A.Entities() order by case when Money > 0 then Money else 0d end, City";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                        new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                    }
                }
            };
            
            var vm = CreateAndRunVirtualMachine(query, sources);
            
            var table = vm.Run();
            
            Assert.AreEqual(5, table.Count);
            Assert.AreEqual("glasgow", table[0].Values[0]);
            Assert.AreEqual("cracow", table[1].Values[0]);
            Assert.AreEqual("katowice", table[2].Values[0]);
            Assert.AreEqual("katowice", table[3].Values[0]);
            Assert.AreEqual("czestochowa", table[4].Values[0]);
        }
        
        [TestMethod]
        public void WhenOrderByMultipleColumnsFirstOneIsCaseWhenDescExpression_ShouldSucceed()
        {
            var query = @"select City from #A.Entities() order by case when Money > 0 then Money else 0d end desc, City desc";
            
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("katowice", "jan", Convert.ToDecimal(300)),
                        new BasicEntity("katowice", "feb", Convert.ToDecimal(100)),
                        new BasicEntity("czestochowa", "jan", Convert.ToDecimal(400)),
                        new BasicEntity("cracow", "jan", Convert.ToDecimal(10)),
                        new BasicEntity("glasgow", "feb", Convert.ToDecimal(-10))
                    }
                }
            };
            
            var vm = CreateAndRunVirtualMachine(query, sources);
            
            var table = vm.Run();
            
            Assert.AreEqual(5, table.Count);
            Assert.AreEqual("czestochowa", table[0].Values[0]);
            Assert.AreEqual("katowice", table[1].Values[0]);
            Assert.AreEqual("katowice", table[2].Values[0]);
            Assert.AreEqual("cracow", table[3].Values[0]);
            Assert.AreEqual("glasgow", table[4].Values[0]);
        }
    }
}
