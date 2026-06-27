using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class InvalidQueryOutputTypeTests
{
    [TestMethod]
    public void WhenSkipWithIntegerLiteral_ShouldSucceed()
    {
        var query = "select Name from #A.Entities() skip 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a"), new BasicEntity("b")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        var name = (string)table[0][0];
        Assert.IsTrue(name == "a" || name == "b", "Result should be one of the input values");
    }

    [TestMethod]
    public void WhenTakeWithIntegerLiteral_ShouldSucceed()
    {
        var query = "select Name from #A.Entities() take 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a"), new BasicEntity("b")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        var name = (string)table[0][0];
        Assert.IsTrue(name == "a" || name == "b", "Result should be one of the input values");
    }

    [TestMethod]
    public void WhenSkipAndTakeWithIntegerLiterals_ShouldSucceed()
    {
        var query = "select Name from #A.Entities() skip 1 take 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a"), new BasicEntity("b"), new BasicEntity("c")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        var name = (string)table[0][0];
        Assert.IsTrue(name == "a" || name == "b" || name == "c", "Result should be one of the input values");
    }



    [TestMethod]
    public void WhenArithmeticExpressionWithPrimitives_ShouldSucceed()
    {
        var query = "select Population + 100, Population * 2 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1100m, table[0][0]);
        Assert.AreEqual(2000m, table[0][1]);
    }

    [TestMethod]
    public void WhenStringConcatenation_ShouldSucceed()
    {
        var query = "select City + ' - ' + Country from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("city1 - country1", table[0][0]);
    }

    [TestMethod]
    public void WhenCaseWhenWithPrimitiveResults_ShouldSucceed()
    {
        var query = "select case when Population > 500 then 'Large' else 'Small' end from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Large", table[0][0]);
    }

    [TestMethod]
    public void WhenCoalesceWithPrimitives_ShouldSucceed()
    {
        var query = "select Coalesce(NullableValue, 0) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }



    [TestMethod]
    public void WhenCountAggregation_ShouldSucceed()
    {
        var query = "select Count(Name) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a"), new BasicEntity("b")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2L, table[0][0]);
    }

    [TestMethod]
    public void WhenSumAggregation_ShouldSucceed()
    {
        var query = "select Sum(Population) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 100), new BasicEntity("city2", "country2", 200)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(300m, table[0][0]);
    }

    [TestMethod]
    public void WhenAvgAggregation_ShouldSucceed()
    {
        var query = "select Avg(Population) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 100), new BasicEntity("city2", "country2", 200)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(150m, table[0][0]);
    }

    [TestMethod]
    public void WhenMinMaxAggregation_ShouldSucceed()
    {
        var query = "select Min(Population), Max(Population) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 100), new BasicEntity("city2", "country2", 200)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100m, table[0][0]);
        Assert.AreEqual(200m, table[0][1]);
    }



    [TestMethod]
    public void WhenUnionWithPrimitiveTypes_ShouldSucceed()
    {
        var query = @"select Name from #A.Entities() union (Name) select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenUnionAllWithPrimitiveTypes_ShouldSucceed()
    {
        var query = @"select Name from #A.Entities() union all (Name) select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void WhenExceptWithPrimitiveTypes_ShouldSucceed()
    {
        var query = @"select Name from #A.Entities() except (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a"), new BasicEntity("b")] },
            { "#B", [new BasicEntity("b")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("a", table[0][0]);
    }

    [TestMethod]
    public void WhenIntersectWithPrimitiveTypes_ShouldSucceed()
    {
        var query = @"select Name from #A.Entities() intersect (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a"), new BasicEntity("b")] },
            { "#B", [new BasicEntity("b"), new BasicEntity("c")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("b", table[0][0]);
    }



    [TestMethod]
    public void WhenCteWithAggregation_ShouldSucceed()
    {
        var query = @"
with totals as (
    select City, Sum(Population) as TotalPop from #A.Entities() group by City
)
select City, TotalPop from totals";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000), new BasicEntity("city1", "country2", 500)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1500m, table[0][1]);
    }

    [TestMethod]
    public void WhenMultipleCteWithPrimitiveTypes_ShouldSucceed()
    {
        var query = @"
with cte1 as (
    select Name, City from #A.Entities()
),
cte2 as (
    select Name, City from cte1
)
select Name, City from cte2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }



    [TestMethod]
    public void WhenInnerJoinWithPrimitiveColumns_ShouldSucceed()
    {
        var query = @"select a.Name, b.City from #A.Entities() a inner join #B.Entities() b on a.City = b.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] },
            { "#B", [new BasicEntity("city1", "country2", 500)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenLeftJoinWithPrimitiveColumns_ShouldSucceed()
    {
        var query = @"select a.Name, b.City from #A.Entities() a left outer join #B.Entities() b on a.City = b.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] },
            { "#B", [new BasicEntity("city2", "country2", 500)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }



    // Note: Subqueries in WHERE are not supported in Musoq.
    // This region is reserved for future subquery support tests.



}
