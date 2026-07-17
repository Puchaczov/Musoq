using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class QualifyTests
{
    [TestMethod]
    public void WhenQualifyWithLeftJoin_ShouldFilterJoinedResultWithNulls()
    {
        var query = @"
            select a.Name, b.City,
                   RowNumber() over (order by a.Name) as rn
            from #A.Entities() a left outer join #B.Entities() b on a.Name = b.Name
            qualify RowNumber() over (order by a.Name) <= 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice"), new BasicEntity("Bob"), new BasicEntity("Charlie")] },
            { "#B", [new BasicEntity("Alice") { City = "NYC" }, new BasicEntity("Charlie") { City = "LA" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.City", typeof(string)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "NYC", 1L],
            ["Bob", null, 2L]);
    }

    [TestMethod]
    public void WhenQualifyWithFrameAndPartition_ShouldFilterCorrectly()
    {
        var query = @"
            select Name, City, Population,
                   Sum(Population) over (partition by City order by Name rows between unbounded preceding and current row) as RunSum
            from #A.Entities()
            qualify Sum(Population) over (partition by City order by Name rows between unbounded preceding and current row) > 100";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC", Population = 50m },
            new BasicEntity("Bob") { City = "NYC", Population = 200m },
            new BasicEntity("Charlie") { City = "LA", Population = 150m });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsGreaterThan(0, table.Count);
        Assert.IsTrue(table.All(r => (decimal)r.Values[3] > 100m));
    }

    [TestMethod]
    public void WhenQualifyDeepNested_CteJoinFrameQualify_ShouldWork()
    {
        var query = @"
            with base as (
                select Name, City, Population
                from #A.Entities()
                where Population > 0
            )
            select b.Name, a.City,
                   Sum(b.Population) over (partition by a.City order by b.Name rows between unbounded preceding and current row) as RunSum
            from base b inner join #A.Entities() a on b.Name = a.Name
            qualify Sum(b.Population) over (partition by a.City order by b.Name rows between unbounded preceding and current row) > 100";

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC", Population = 50m },
            new BasicEntity("Bob") { City = "NYC", Population = 200m },
            new BasicEntity("Charlie") { City = "LA", Population = 150m });

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);
        var rows = table
            .Select(static row => new
            {
                Name = (string)row.Values[0],
                City = (string)row.Values[1],
                RunSum = (decimal)row.Values[2]
            })
            .OrderBy(static row => row.Name)
            .ToArray();

        Assert.AreEqual(2, rows.Length);
        Assert.AreEqual("Bob", rows[0].Name);
        Assert.AreEqual("NYC", rows[0].City);
        Assert.AreEqual(250m, rows[0].RunSum);
        Assert.AreEqual("Charlie", rows[1].Name);
        Assert.AreEqual("LA", rows[1].City);
        Assert.AreEqual(150m, rows[1].RunSum);
    }

    [TestMethod]
    public void WhenQualifyWithoutWindowFunction_ShouldThrowMQ3050()
    {
        const string query = @"
            select Name
            from #A.Entities()
            qualify Name = 'Alice'";

        var sources = CreateSingleSource(
            new BasicEntity("Alice"),
            new BasicEntity("Bob"));

        var ex = Assert.Throws<MusoqQueryException>(() =>
        {
            var vm = CreateAndRunVirtualMachine(query, sources);
            vm.Run(TestContext.CancellationToken);
        });

        Assert.AreEqual(DiagnosticCode.MQ3050_QualifyRequiresWindowFunction, ex.PrimaryEnvelope.Code);
    }
}
