using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class SetsOperatorsTests
{

    [TestMethod]
    public void UnionWithDifferentColumnsAsAKeyTest()
    {
        var query = @"select Name from #A.Entities() union (Name) select City as Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003", "", 0), new BasicEntity("004", "", 0)] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count, "Table should contain 4 rows");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "001"), "Missing 001");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "002"), "Missing 002");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "003"), "Missing 003");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "004"), "Missing 004");
    }

    [TestMethod]
    public void AliasedUnionWithDifferentColumnsAsAKeyTest()
    {
        var query =
            """
            select
                a.Name as a1,
                b.Value
            from #A.Entities() a
            cross apply a.ToCharArray(a.Name) b
            union (Name)
            select
                a.Name as a1,
                b.Value
            from #A.Entities() a
            cross apply a.ToCharArray(a.Name) b
            """;
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.Run(TestContext.CancellationToken);
    }

    [TestMethod]
    public void UnionWithSkipTest()
    {
        var query = @"select Name from #A.Entities() skip 1 union (Name) select Name from #B.Entities() skip 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "First entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Second entry should be '005'");
    }

    [TestMethod]
    public void UnionAllWithSkipTest()
    {
        var query = @"select Name from #A.Entities() skip 1 union all (Name) select Name from #B.Entities() skip 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("005")] },
            { "#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.All(entry => (string)entry.Values[0] == "005"), "All entries should be '005'");
    }

    [TestMethod]
    public void MultipleUnionAllTest()
    {
        var query = @"
select Name from #A.Entities() union all (Name)
select Name from #A.Entities() union all (Name)
select Name from #A.Entities() union all (Name)
select Name from #A.Entities() union all (Name)
select Name from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(5, table.Count, "Table should have 5 entries");

        Assert.IsTrue(table.All(entry =>
                (string)entry.Values[0] == "005"),
            "All entries should be '005'");
    }

    [TestMethod]
    public void UnionAllWhenMultipleSelectsCombinedWithUnionAllWithinCteExpression_ShouldSucceed()
    {
        var query = @"
with p as (
    select 1 as Id, 'EMPTY' as Name from #A.Entities()
    union all (Name)
    select 2 as Id, 'EMPTY2' as Name from #A.Entities()
    union all (Name)
    select 3 as Id, 'EMPTY3' as Name from #A.Entities()
)
select Id, Name from p
";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry =>
                Convert.ToInt32(entry.Values[0]) == 1 &&
                (string)entry.Values[1] == "EMPTY"),
            "First entry should match expected values");

        Assert.IsTrue(table.Any(entry =>
                Convert.ToInt32(entry.Values[0]) == 2 &&
                (string)entry.Values[1] == "EMPTY2"),
            "Second entry should match expected values");

        Assert.IsTrue(table.Any(entry =>
                Convert.ToInt32(entry.Values[0]) == 3 &&
                (string)entry.Values[1] == "EMPTY3"),
            "Third entry should match expected values");
    }

    [TestMethod]
    public void UnionWhenMultipleSelectsCombinedWithUnionWithinCteExpression_ShouldSucceed()
    {
        var query = @"
with p as (
    select 1 as Id, 'EMPTY' as Name from #A.Entities()
    union (Name)
    select 2 as Id, 'EMPTY2' as Name from #A.Entities()
    union (Name)
    select 3 as Id, 'EMPTY3' as Name from #A.Entities()
)
select Id, Name from p
";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry =>
                Convert.ToInt32(entry.Values[0]) == 1 &&
                (string)entry.Values[1] == "EMPTY"),
            "First entry should match expected values");

        Assert.IsTrue(table.Any(entry =>
                Convert.ToInt32(entry.Values[0]) == 2 &&
                (string)entry.Values[1] == "EMPTY2"),
            "Second entry should match expected values");

        Assert.IsTrue(table.Any(entry =>
                Convert.ToInt32(entry.Values[0]) == 3 &&
                (string)entry.Values[1] == "EMPTY3"),
            "Third entry should match expected values");
    }

    [TestMethod]
    public void MultipleUnionAllWithSkipTest()
    {
        var query = @"
select Name from #A.Entities() skip 1
union all (Name)
select Name from #B.Entities() skip 2
union all (Name)
select Name from #C.Entities() skip 3";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("005")] },
            { "#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")] },
            {
                "#C",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("004"), new BasicEntity("005")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.All(entry => (string)entry.Values[0] == "005"), "All entries should be '005'");
    }

    [TestMethod]
    public void UnionWithoutDuplicatedKeysTest()
    {
        var query = @"select Name from #A.Entities() union (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count, "Table should contain 4 rows");

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "001") &&
                      table.Any(row => (string)row.Values[0] == "002") &&
                      table.Any(row => (string)row.Values[0] == "003") &&
                      table.Any(row => (string)row.Values[0] == "004"),
            "Expected rows with values 001, 002, 003, and 004");
    }

    [TestMethod]
    public void UnionWithDuplicatedKeysTest()
    {
        var query = @"select Name from #A.Entities() union (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Second entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Third entry should be '005'");
    }

    [TestMethod]
    public void MultipleUnionsWithDuplicatedKeysTest()
    {
        var query =
            @"select Name from #A.Entities() union (Name) select Name from #B.Entities() union (Name) select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#C", [new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Second entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Third entry should be '005'");
    }

    [TestMethod]
    public void MultipleUnionsWithoutDuplicatedKeysTest()
    {
        var query =
            @"select Name from #A.Entities() union (Name) select Name from #B.Entities() union (Name) select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
            { "#C", [new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "Table should have 3 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Second entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Third entry should be '005'");
    }

    [TestMethod]
    public void MultipleUnionsComplexTest()
    {
        var query =
            @"
select Name from #A.Entities() union (Name)
select Name from #B.Entities() union (Name)
select Name from #C.Entities() union (Name)
select Name from #D.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
            { "#C", [new BasicEntity("005")] },
            { "#D", [new BasicEntity("007"), new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count, "Table should have 4 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Second entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Third entry should be '005'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "007"), "Fourth entry should be '007'");
    }

    [TestMethod]
    public void UnionAllWithDuplicatedKeysTest()
    {
        var query = @"select Name from #A.Entities() union all (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(5, table.Count, "Table should have 5 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Second entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "Third entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Fourth entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Fifth entry should be '005'");
    }

    [TestMethod]
    public void MultipleUnionsAllWithDuplicatedKeysTest()
    {
        var query =
            @"select Name from #A.Entities() union all (Name) select Name from #B.Entities() union all (Name) select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#C", [new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);
        var results = table.Select(row => (string)row.Values[0]).ToList();
        Assert.AreEqual(2, results.Count(r => r == "001"), "Should have two '001' entries");
        Assert.AreEqual(1, results.Count(r => r == "002"), "Should have one '002' entry");
        Assert.AreEqual(1, results.Count(r => r == "005"), "Should have one '005' entry");
    }

    [TestMethod]
    public void MultipleUnionsAllWithoutDuplicatedKeysTest()
    {
        var query =
            @"select Name from #A.Entities() union all (Name) select Name from #B.Entities() union all (Name) select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
            { "#C", [new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => (string)row.Values[0]).ToList();
        CollectionAssert.Contains(results, "001");
        CollectionAssert.Contains(results, "002");
        CollectionAssert.Contains(results, "005");
    }

    [TestMethod]
    public void MultipleUnionsAllComplexTest()
    {
        var query =
            @"
select Name from #A.Entities() union all (Name)
select Name from #B.Entities() union all (Name)
select Name from #C.Entities() union all (Name)
select Name from #D.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
            { "#C", [new BasicEntity("005")] },
            { "#D", [new BasicEntity("007"), new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(5, table.Count, "Table should have 5 entries");
        var results = table.Select(row => (string)row.Values[0]).ToList();
        Assert.AreEqual(2, results.Count(r => r == "001"), "Should have two '001' entries");
        Assert.AreEqual(1, results.Count(r => r == "002"), "Should have one '002' entry");
        Assert.AreEqual(1, results.Count(r => r == "005"), "Should have one '005' entry");
        Assert.AreEqual(1, results.Count(r => r == "007"), "Should have one '007' entry");
    }

    [TestMethod]
    public void UnionAllWithoutDuplicatedKeysTest()
    {
        var query = @"select Name from #A.Entities() union all (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(5, table.Count, "Table should contain 5 rows");

        Assert.IsTrue(table.Count(row => (string)row.Values[0] == "001") == 2 &&
                      table.Any(row => (string)row.Values[0] == "002") &&
                      table.Any(row => (string)row.Values[0] == "003") &&
                      table.Any(row => (string)row.Values[0] == "004"),
            "Expected two rows with 001 and one row each with 002, 003, and 004");
    }

    [TestMethod]
    public void UnionSameSourceTest()
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
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Second entry should be '002'");
    }

    [TestMethod]
    public void UnionMultipleTimesSameSourceTest()
    {
        var query =
            @"
select Name from #A.Entities() where Name = '001'
union (Name)
select Name from #A.Entities() where Name = '002'
union (Name)
select Name from #A.Entities() where Name = '003'
union (Name)
select Name from #A.Entities() where Name = '004'
union (Name)
select Name from #A.Entities() where Name = '005'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"),
                    new BasicEntity("005")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(5, table.Count, "Table should have 5 entries");

        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "001"), "First entry should be '001'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "002"), "Second entry should be '002'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "003"), "Third entry should be '003'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "004"), "Fourth entry should be '004'");
        Assert.IsTrue(table.Any(entry => (string)entry.Values[0] == "005"), "Fifth entry should be '005'");
    }

    [TestMethod]
    public void WhenWrongTypeBetweenUnions_ShouldFail()
    {
        var query = @"select Name from #A.Entities() union (Name) select 1 as Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3020_SetOperatorColumnTypes, DiagnosticPhase.Bind, "same types");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenWrongTypeBetweenUnionAll_ShouldFail()
    {
        var query = @"select Name from #A.Entities() union all (Name) select 1 as Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3020_SetOperatorColumnTypes, DiagnosticPhase.Bind, "same types");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenUnionHasEmptyKeyList_ShouldUseAllProjectedFields()
    {
        var query = @"select Name from #A.Entities() union () select Name as Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenUnionAllHasEmptyKeyList_ShouldAppendRows()
    {
        var query = @"select Name from #A.Entities() union all () select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.All(row => (string)row.Values[0] == "001"));
    }

}
