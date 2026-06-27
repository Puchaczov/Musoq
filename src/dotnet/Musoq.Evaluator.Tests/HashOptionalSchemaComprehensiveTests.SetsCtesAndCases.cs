using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class HashOptionalSchemaComprehensiveTests
{
    [TestMethod]
    public void HashOptional_CteWithGroupBy_ShouldWork()
    {
        var query = @"
            with cte as (
                select Country, Sum(Population) as TotalPop
                from A.Entities()
                group by Country
            )
            select Country, TotalPop from cte";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Country = "Poland", Population = 100 },
                    new BasicEntity { Country = "Poland", Population = 200 },
                    new BasicEntity { Country = "Germany", Population = 150 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_CteWithSetOperators_ShouldWork()
    {
        var query = @"
            with cte as (
                select Name from A.Entities()
                union (Name)
                select Name from B.Entities()
            )
            select Name from cte";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("First")] },
            { "#B", [new BasicEntity("Second")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_CteMixedHashAndNoHash_ShouldWork()
    {
        var query = @"
            with cte1 as (select Name from #A.Entities()),
            cte2 as (select Name from B.Entities())
            select * from cte1 union (Name) select * from cte2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("HashSyntax")] },
            { "#B", [new BasicEntity("NoHashSyntax")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }



    [TestMethod]
    public void HashOptional_CaseWhenSimple_ShouldWork()
    {
        var query = "select case when Population > 100 then 'High' else 'Low' end as Category from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Population = 50 },
                    new BasicEntity { Population = 150 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(r => (string)r[0] == "Low"));
        Assert.IsTrue(table.Any(r => (string)r[0] == "High"));
    }

    [TestMethod]
    public void HashOptional_CaseWhenMultipleBranches_ShouldWork()
    {
        var query = @"
            select case
                when Population < 50 then 'Small'
                when Population < 100 then 'Medium'
                else 'Large'
            end as Size from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Population = 30 },
                    new BasicEntity { Population = 75 },
                    new BasicEntity { Population = 150 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
    }



    [TestMethod]
    public void HashOptional_ReorderedQueryBasic_ShouldWork()
    {
        var query = "from A.Entities() select Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test1"), new BasicEntity("Test2")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_ReorderedQueryWithWhere_ShouldWork()
    {
        var query = "from A.Entities() where Name = 'Match' select Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Match"), new BasicEntity("NoMatch")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Match", table[0][0]);
    }

    [TestMethod]
    public void HashOptional_ReorderedQueryWithGroupBy_ShouldWork()
    {
        var query = "from A.Entities() group by City select City, Count(City)";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { City = "Warsaw" },
                    new BasicEntity { City = "Warsaw" },
                    new BasicEntity { City = "Berlin" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_ReorderedQueryWithJoin_ShouldWork()
    {
        var query = "from A.Entities() a inner join B.Entities() b on a.Name = b.Name select a.Name, b.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Match")] },
            { "#B", [new BasicEntity("Match") { City = "Warsaw" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Match", table[0][0]);
        Assert.AreEqual("Warsaw", table[0][1]);
    }



    [TestMethod]
    public void HashOptional_ComplexQueryAllFeatures_ShouldWork()
    {
        var query = @"
            with filtered as (
                select Name, City, Population
                from A.Entities()
                where Population > 50
            )
            select
                City,
                Count(City) as CityCount,
                Sum(Population) as TotalPop
            from filtered
            group by City
            having Count(City) > 0
            order by Sum(Population) desc
            take 10";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", City = "Warsaw", Population = 100 },
                    new BasicEntity { Name = "B", City = "Warsaw", Population = 150 },
                    new BasicEntity { Name = "C", City = "Berlin", Population = 200 },
                    new BasicEntity { Name = "D", City = "Berlin", Population = 30 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_MultipleOperationsChained_ShouldWork()
    {
        var query = @"
            select a.Name, b.Name, c.Name
            from A.Entities() a
            inner join B.Entities() b on a.Name = b.Name
            inner join C.Entities() c on b.Name = c.Name
            where a.Population > 50
            order by a.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "Match", Population = 100 }] },
            { "#B", [new BasicEntity("Match")] },
            { "#C", [new BasicEntity("Match")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
    }



    [TestMethod]
    public void HashOptional_StringFunctions_ShouldWork()
    {
        var query = "select ToUpper(Name), ToLower(Name), Length(Name) from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("TEST", table[0][0]);
        Assert.AreEqual("test", table[0][1]);
        Assert.AreEqual(4, table[0][2]);
    }

    [TestMethod]
    public void HashOptional_CoalesceFunction_ShouldWork()
    {
        var query = "select Coalesce(NullableValue, 999) from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A") { NullableValue = 5 },
                    new BasicEntity("B") { NullableValue = null }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }



    [TestMethod]
    public void HashOptional_DescSchema_ShouldWork()
    {
        var query = "desc A";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.IsGreaterThan(0, table.Count, "Should return at least one method");
        Assert.IsTrue(table.Any(row => (string)row[0] == "entities"), "Should contain 'entities' method");
    }

    [TestMethod]
    public void HashOptional_DescSchemaMethod_ShouldWork()
    {
        var query = "desc A.entities";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(1, table.Count, "Should return exactly one method name");
        Assert.AreEqual("entities", table[0][0], "Should return the method name");
    }

}
