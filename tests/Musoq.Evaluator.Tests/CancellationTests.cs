using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CancellationTests : BasicEntityTestBase
{
    [TestMethod]
    public void QueryCancellation()
    {
        var query = @"select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        TokenSource.Cancel();
        Assert.Throws<OperationCanceledException>(() => vm.Run(TokenSource.Token));
    }

    [TestMethod]
    public void GroupByQueryCancellation()
    {
        var query = @"select Name, Count(Name) from #A.Entities() group by Name having Count(Name) >= 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ABBA")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        TokenSource.Cancel();
        Assert.Throws<OperationCanceledException>(() => vm.Run(TokenSource.Token));
    }


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
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        TokenSource.Cancel();
        Assert.Throws<OperationCanceledException>(() => vm.Run(TokenSource.Token));
    }

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
                [
                    new BasicEntity("001", "", 100), new BasicEntity("001", "", 100),
                    new BasicEntity("002", "", 500)
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("003", "", 13), new BasicEntity("003", "", 13), new BasicEntity("003", "", 13)
                ]
            },
            { "#C", [new BasicEntity("002", "", 14), new BasicEntity("002", "", 14)] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        TokenSource.Cancel();
        Assert.Throws<OperationCanceledException>(() => vm.Run(TokenSource.Token));
    }

    [TestMethod]
    public void IntersectQueryCancellation()
    {
        var query =
            @"select Name from #A.Entities() intersect (Name) select Name from #B.Entities() intersect (Name) select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")] },
            { "#C", [new BasicEntity("002"), new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        TokenSource.Cancel();
        Assert.Throws<OperationCanceledException>(() => vm.Run(TokenSource.Token));
    }


    [TestMethod]
    public void UnionAllQueryCancellation()
    {
        var query = @"select Name from #A.Entities() union all (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        TokenSource.Cancel();
        Assert.Throws<OperationCanceledException>(() => vm.Run(TokenSource.Token));
    }

    [TestMethod]
    public void CteQueryCancellation()
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
        TokenSource.Cancel();
        Assert.Throws<OperationCanceledException>(() => vm.Run(TokenSource.Token));
    }
}
