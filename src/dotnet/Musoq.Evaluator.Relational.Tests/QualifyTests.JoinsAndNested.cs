using System.Collections.Generic;
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
        var query = (
            "\n"
            + "            select a.Name, b.City,\n"
            + "                   RowNumber() over (order by a.Name) as rn\n"
            + "            from #A.Entities() a left outer join #B.Entities() b on a.Name = b.Name\n"
            + "            qualify RowNumber() over (order by a.Name) <= 2"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Alice"), new BasicEntity("Bob"), new BasicEntity("Charlie")] },
            { "#B", [new BasicEntity("Alice") { City = "NYC" }, new BasicEntity("Charlie") { City = "LA" }] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("rn", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Alice", "NYC", 1L], ["Bob", null, 2L]);
    }

    [TestMethod]
    public void WhenQualifyWithFrameAndPartition_ShouldFilterCorrectly()
    {
        var query = (
            "\n"
            + "            select Name, City, Population,\n"
            + "                   Sum(Population) over (partition by City order by Name rows between unbounded preceding and current row) as RunSum\n"
            + "            from #A.Entities()\n"
            + "            qualify Sum(Population) over (partition by City order by Name rows between unbounded preceding and current row) > 100"
        );

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC", Population = 50m },
            new BasicEntity("Bob") { City = "NYC", Population = 200m },
            new BasicEntity("Charlie") { City = "LA", Population = 150m }
        );

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("Population", typeof(decimal)),
            ("RunSum", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Bob", "NYC", 200m, 250m],
            ["Charlie", "LA", 150m, 150m]
        );
    }

    [TestMethod]
    public void WhenQualifyDeepNested_CteJoinFrameQualify_ShouldWork()
    {
        var query = (
            "\n"
            + "            with base as (\n"
            + "                select Name, City, Population\n"
            + "                from #A.Entities()\n"
            + "                where Population > 0\n"
            + "            )\n"
            + "            select b.Name, a.City,\n"
            + "                   Sum(b.Population) over (partition by a.City order by b.Name rows between unbounded preceding and current row) as RunSum\n"
            + "            from base b inner join #A.Entities() a on b.Name = a.Name\n"
            + "            qualify Sum(b.Population) over (partition by a.City order by b.Name rows between unbounded preceding and current row) > 100"
        );

        var sources = CreateSingleSource(
            new BasicEntity("Alice") { City = "NYC", Population = 50m },
            new BasicEntity("Bob") { City = "NYC", Population = 200m },
            new BasicEntity("Charlie") { City = "LA", Population = 150m }
        );

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("RunSum", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Bob", "NYC", 250m], ["Charlie", "LA", 150m]);
    }

    [TestMethod]
    public void WhenQualifyWithoutWindowFunction_ShouldThrowMQ3050()
    {
        const string query = (
            "\n"
            + "            select Name\n"
            + "            from #A.Entities()\n"
            + "            qualify Name = 'Alice'"
        );

        var sources = CreateSingleSource(new BasicEntity("Alice"), new BasicEntity("Bob"));

        var ex = Assert.Throws<MusoqQueryException>(() =>
        {
            var vm = CreateAndRunVirtualMachine(query, sources);
            vm.Run(TestContext.CancellationToken);
        });

        Assert.AreEqual(DiagnosticCode.MQ3050_QualifyRequiresWindowFunction, ex.PrimaryEnvelope.Code);
    }
}
