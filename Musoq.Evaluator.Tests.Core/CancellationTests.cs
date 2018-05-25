using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Core.Schema;

namespace Musoq.Evaluator.Tests.Core
{
    [TestClass]
    public class CancellationTests : TestBase
    {
        [ExpectedException(typeof(OperationCanceledException))]
        [TestMethod]
        public void QueryCancellation()
        {
            var query = @"select Name from #A.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            TokenSource.Cancel();
            vm.Run(TokenSource.Token);
        }

        [ExpectedException(typeof(OperationCanceledException))]
        [TestMethod]
        public void GroupByQueryCancellation()
        {
            var query = @"select Name, Count(Name) from #A.Entities() group by Name having Count(Name) >= 2";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", new[]
                    {
                        new BasicEntity("ABBA")
                    }
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            TokenSource.Cancel();
            var table = vm.Run(TokenSource.Token);
        }


        [ExpectedException(typeof(OperationCanceledException))]
        [TestMethod]
        public void UnionQueryCancellation()
        {
            var query =
                @"
select Name from #A.Entities() where Name = '001'
union (Name)
select Name from #A.Entities() where Name = '002'";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            TokenSource.Cancel();
            vm.Run(TokenSource.Token);
        }

        [ExpectedException(typeof(OperationCanceledException))]
        [TestMethod]
        public void ExceptQueryCancellation()
        {
            var query =
                @"select City, Sum(Population) from #A.Entities() group by City
except (City)
select City, Sum(Population) from #B.Entities() group by City
except (City)
select City, Sum(Population) from #C.Entities() group by City";

            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    new[]
                    {
                        new BasicEntity("001", "", 100), new BasicEntity("001", "", 100),
                        new BasicEntity("002", "", 500)
                    }
                },
                {
                    "#B",
                    new[]
                    {
                        new BasicEntity("003", "", 13), new BasicEntity("003", "", 13), new BasicEntity("003", "", 13)
                    }
                },
                {"#C", new[] {new BasicEntity("002", "", 14), new BasicEntity("002", "", 14)}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            TokenSource.Cancel();
            vm.Run(TokenSource.Token);
        }

        [ExpectedException(typeof(OperationCanceledException))]
        [TestMethod]
        public void IntersectQueryCancellation()
        {
            var query =
                @"select Name from #A.Entities() intersect (Name) select Name from #B.Entities() intersect (Name) select Name from #C.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")}},
                {"#C", new[] {new BasicEntity("002"), new BasicEntity("001")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            TokenSource.Cancel();
            vm.Run(TokenSource.Token);
        }


        [ExpectedException(typeof(OperationCanceledException))]
        [TestMethod]
        public void UnionAllQueryCancellation()
        {
            var query = @"select Name from #A.Entities() union all (Name) select Name from #B.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}},
                {"#B", new[] {new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            TokenSource.Cancel();
            vm.Run(TokenSource.Token);
        }

        [ExpectedException(typeof(OperationCanceledException))]
        [TestMethod]
        public void CteQueryCancellation()
        {
            var query = "with p as (select City, Country from #A.entities()) select Country, City from p";

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
            TokenSource.Cancel();
            vm.Run(TokenSource.Token);
        }
    }
}
