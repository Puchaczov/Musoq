using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class HashJoinCompositeKeysTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void InnerJoin_WithWideCompositeKeys_ShouldUseNestedTypedTuples()
    {
        foreach (var keyWidth in new[] { 8, 9, 15 })
        {
            var fields = new[]
            {
                "Name", "City", "Country", "Population", "Month", "Money", "Id", "NullableValue"
            };
            var conditions = Enumerable.Range(0, keyWidth)
                .Select(index =>
                {
                    var field = fields[index % fields.Length];
                    return $"a.{field} = b.{field}";
                });
            var query = $"select a.Name, b.Name from #A.entities() a inner join #B.entities() b on {string.Join(" AND ", conditions)}";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [new BasicEntity { Name = "match", City = "NY", Country = "PL", Population = 10m, Month = "Jan", Money = 20m, Id = 1, NullableValue = 2 }],
                ["#B"] = [new BasicEntity { Name = "match", City = "NY", Country = "PL", Population = 10m, Month = "Jan", Money = 20m, Id = 1, NullableValue = 2 }]
            };

            var inspection = InstanceCreator.CompileForInspection(
                query,
                Guid.NewGuid().ToString(),
                new BasicSchemaProvider<BasicEntity>(sources),
                LoggerResolver,
                new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));
            Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("CreateNullableHashJoinKey", StringComparison.Ordinal));
            Assert.Contains("ValueTuple<", inspection.GeneratedCSharpCode);

            var table = InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                new BasicSchemaProvider<BasicEntity>(sources),
                LoggerResolver,
                new CompilationOptions(useHashJoin: true, useSortMergeJoin: false))
                .Run(TestContext.CancellationToken);
            Assert.AreEqual(1, table.Count, $"key width {keyWidth}");
            Assert.AreEqual("match", table[0][0]);
            Assert.AreEqual("match", table[0][1]);
        }
    }

    [TestMethod]
    public void InnerJoin_WithCompositeKey_ShouldUseHashJoin()
    {
        const string query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b 
on a.Country = b.Country AND a.City = b.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "John", City = "New York", Country = "USA" },
                    new BasicEntity { Name = "Alice", City = "London", Country = "UK" },
                    new BasicEntity { Name = "Bob", City = "Paris", Country = "France" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Doe", City = "New York", Country = "USA" },
                    new BasicEntity { Name = "Smith", City = "London", Country = "UK" },
                    new BasicEntity { Name = "Pierre", City = "Lyon", Country = "France" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: true));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should have 2 matches");

        var rows = table.OrderBy(r => r[0]).ToList();

        Assert.AreEqual("Alice", rows[0][0]);
        Assert.AreEqual("Smith", rows[0][1]);

        Assert.AreEqual("John", rows[1][0]);
        Assert.AreEqual("Doe", rows[1][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenCompositeKeyUsesValueTypes_ShouldUseValueTupleHashKey()
    {
        const string query = @"
select
    a.Name,
    b.Name
from #A.entities() a
inner join #B.entities() b
on a.Id = b.Id AND a.Population = b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "John", Id = 1, Population = 100 },
                    new BasicEntity { Name = "Alice", Id = 2, Population = 200 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Doe", Id = 1, Population = 100 },
                    new BasicEntity { Name = "Smith", Id = 2, Population = 300 }
                ]
            }
        };

        var result = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            new CompilationOptions(useHashJoin: true, useSortMergeJoin: false));

        Assert.Contains("ExecutionPlan [compiled]", result.ExecutionPlanText);
        Assert.Contains(
            "CreateHash [bHash: ValueTuple<int, decimal> -> BasicEntity]",
            result.ExecutionPlanText);
        Assert.Contains("HashAdd [bHash[(b.Id, b.Population)] += b]", result.ExecutionPlanText);
        Assert.Contains("HashProbe [bHash[(a.Id, a.Population)] -> bHashMatches]", result.ExecutionPlanText);
        Assert.Contains("new Dictionary<ValueTuple<int, decimal>, HashJoinBucket<", result.GeneratedCSharpCode);
        Assert.Contains("var key = (b.Id, b.Population);", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("CreateNullableHashJoinKey", StringComparison.Ordinal));
    }

    [TestMethod]
    public void LeftOuterJoin_WithCompositeKey_ShouldUseHashJoin()
    {
        const string query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
left outer join #B.entities() b 
on a.Country = b.Country AND a.City = b.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "John", City = "New York", Country = "USA" },
                    new BasicEntity { Name = "Bob", City = "Paris", Country = "France" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Doe", City = "New York", Country = "USA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: true));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => r[0]).ToList();

        Assert.AreEqual("Bob", rows[0][0]);
        Assert.IsNull(rows[0][1]);

        Assert.AreEqual("John", rows[1][0]);
        Assert.AreEqual("Doe", rows[1][1]);
    }

    [TestMethod]
    public void RightOuterJoin_WithCompositeKey_ShouldUseHashJoin()
    {
        const string query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
right outer join #B.entities() b 
on a.Country = b.Country AND a.City = b.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "John", City = "New York", Country = "USA" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Doe", City = "New York", Country = "USA" },
                    new BasicEntity { Name = "Pierre", City = "Paris", Country = "France" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: true));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => r[1]).ToList();

        Assert.AreEqual("John", rows[0][0]);
        Assert.AreEqual("Doe", rows[0][1]);

        Assert.IsNull(rows[1][0]);
        Assert.AreEqual("Pierre", rows[1][1]);
    }

    [TestMethod]
    public void InnerJoin_WithNullsInCompositeKey_ShouldNotMatch()
    {
        const string query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b 
on a.Country = b.Country AND a.City = b.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "John", City = null, Country = "USA" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Doe", City = null, Country = "USA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: true));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "Should have 0 matches because NULL != NULL");
    }

    [TestMethod]
    public void InnerJoin_WithThreeCompositeKeys_ShouldWork()
    {
        const string query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b 
on a.Country = b.Country AND a.City = b.City AND a.Month = b.Month";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "John", City = "NY", Country = "USA", Month = "Jan" },
                    new BasicEntity { Name = "Alice", City = "NY", Country = "USA", Month = "Feb" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Doe", City = "NY", Country = "USA", Month = "Jan" },
                    new BasicEntity { Name = "Smith", City = "NY", Country = "USA", Month = "Mar" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: true));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("John", table[0][0]);
        Assert.AreEqual("Doe", table[0][1]);
    }

    [TestMethod]
    public void LeftOuterJoin_WithEmptySource_ShouldReturnAllLeft()
    {
        const string query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
left outer join #B.entities() b 
on a.Country = b.Country AND a.City = b.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "John", City = "NY", Country = "USA" }
                ]
            },
            {
                "#B", new List<BasicEntity>()
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: true));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("John", table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    [TestMethod]
    public void LeftOuterJoin_WithPostJoinFilterOnNullableSide_ShouldFilterNullExtendedRows()
    {
        const string query = @"
select
    a.Name,
    b.Name
from #A.entities() a
left outer join #B.entities() b
on a.Country = b.Country
where b.Name is null";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "John", Country = "USA" },
                    new BasicEntity { Name = "Bob", Country = "France" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Doe", Country = "USA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: true));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Bob", table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    [TestMethod]
    public void InnerJoin_WithAdditionalNonEquiCondition()
    {
        const string query = @"
select 
    a.Name, 
    b.Name 
from #A.entities() a 
inner join #B.entities() b 
on a.Country = b.Country AND a.City = b.City AND a.Population > b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "BigCity", City = "NY", Country = "USA", Population = 1000 }
                ]
            },
            {
                "#B", [
                    new BasicEntity
                        { Name = "SmallCity", City = "NY", Country = "USA", Population = 100 },
                    new BasicEntity
                        { Name = "HugeCity", City = "NY", Country = "USA", Population = 2000 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, new CompilationOptions(useHashJoin: true));
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("BigCity", table[0][0]);
        Assert.AreEqual("SmallCity", table[0][1]);
    }

    private CompiledQuery CreateAndRunVirtualMachine(
        string script,
        IDictionary<string, IEnumerable<BasicEntity>> sources,
        CompilationOptions options)
    {
        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            options);
    }
}
