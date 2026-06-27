using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class EndToEndQueryExecutionTests
{
    [TestMethod]
    public void Query_NestedArithmetic_ShouldWork()
    {
        var query = "select ((Population * 2) + 10) / 5 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void Query_StringFunctions_ShouldWork()
    {
        var query = "select ToUpperInvariant(Name), ToLowerInvariant(City), Length(Name) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "Test", City = "NYC" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("TEST", table[0][0]);
        Assert.AreEqual("nyc", table[0][1]);
        Assert.AreEqual(4, Convert.ToInt32(table[0][2]));
    }

    [TestMethod]
    public void Query_NullCoalesce_ShouldWork()
    {
        var query = "select Coalesce(NullableValue, -1) from #A.Entities()";
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
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }



    [TestMethod]
    public void Query_OrderByMultipleColumns_ShouldWork()
    {
        var query = "select Name, City from #A.Entities() order by City asc, Name desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "B", City = "NYC" },
                    new BasicEntity { Name = "A", City = "NYC" },
                    new BasicEntity { Name = "C", City = "LA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void Query_OrderByWithNulls_ShouldWork()
    {
        var query = "select Name, NullableValue from #A.Entities() order by NullableValue";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A") { NullableValue = 10 },
                    new BasicEntity("B") { NullableValue = null },
                    new BasicEntity("C") { NullableValue = 5 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void Query_OrderByExpression_ShouldWork()
    {
        var query = "select Name, Population from #A.Entities() order by Population";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "B", Population = 200 },
                    new BasicEntity { Name = "A", Population = 100 },
                    new BasicEntity { Name = "C", Population = 50 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
    }



    [TestMethod]
    public void Query_ImplicitTypeConversion_ShouldWork()
    {
        var query = "select Population + 1.5 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void Query_ToString_ShouldWork()
    {
        var query = "select ToString(Population) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("100", table[0][0]);
    }

    [TestMethod]
    public void Query_ToInt32_ShouldWork()
    {
        var query = "select ToInt32('123') from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(123, table[0][0]);
    }



    [TestMethod]
    public void Query_EmptyResult_ShouldWork()
    {
        var query = "select Name from #A.Entities() where 1 = 0";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void Query_NoSourceRows_ShouldWork()
    {
        var query = "select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void Query_VeryLongInList_ShouldWork()
    {
        var inList = string.Join(", ", Enumerable.Range(1, 50).Select(i => $"'{i}'"));
        var query = $"select Name from #A.Entities() where Name in ({inList})";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("25"), new BasicEntity("100")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("25", table[0][0]);
    }



    [TestMethod]
    public void Query_BooleanExpression_ShouldWork()
    {
        var query = "select Population > 100 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [new BasicEntity { Name = "A", Population = 50 }, new BasicEntity { Name = "B", Population = 200 }]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Query_AllComparisonOperators_ShouldWork()
    {
        var query = @"
            select
                Population > 100,
                Population >= 100,
                Population < 100,
                Population <= 100,
                Population = 100,
                Population <> 100
            from #A.Entities()
            where Population = 100";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
    }



    [TestMethod]
    public void Query_SkipOnly_ShouldWork()
    {
        var query = "select Name from #A.Entities() order by Name skip 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("C"), new BasicEntity("D")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Query_TakeOnly_ShouldWork()
    {
        var query = "select Name from #A.Entities() order by Name take 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("C"), new BasicEntity("D")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Query_SkipMoreThanRows_ShouldReturnEmpty()
    {
        var query = "select Name from #A.Entities() order by Name skip 100";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void Query_SkipAndTake_ShouldWork()
    {
        var query = "select Name from #A.Entities() order by Name skip 1 take 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("C"), new BasicEntity("D")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }



}
