using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class SetsOperatorsTests
{
    [TestMethod]
    public void UnionWithOmittedKeys_ShouldCompareAllProjectedValues()
    {
        var query = (
            "\n"
            + "            select Name, City from #A.Entities()\n"
            + "            union\n"
            + "            select Name, City from #B.Entities()"
        );

        var table = CreateAndRunVirtualMachine(query, CreateSameNameDifferentCitySources())
            .Run(TestContext.CancellationToken);

        AssertNameCityRows(table, ["001", "Warsaw"], ["001", "Berlin"]);
    }

    [TestMethod]
    public void UnionWithEmptyKeys_ShouldCompareAllProjectedValues()
    {
        var query = (
            "\n"
            + "            select Name, City from #A.Entities()\n"
            + "            union ()\n"
            + "            select Name, City from #B.Entities()"
        );

        var table = CreateAndRunVirtualMachine(query, CreateSameNameDifferentCitySources())
            .Run(TestContext.CancellationToken);

        AssertNameCityRows(table, ["001", "Warsaw"], ["001", "Berlin"]);
    }

    [TestMethod]
    public void UnionAllWithOmittedKeys_ShouldAppendRows()
    {
        var query = (
            "\n"
            + "            select Name, City from #A.Entities()\n"
            + "            union all\n"
            + "            select Name, City from #B.Entities()"
        );

        var table = CreateAndRunVirtualMachine(query, CreateSameNameDifferentCitySources())
            .Run(TestContext.CancellationToken);

        AssertNameCityRows(table, ["001", "Warsaw"], ["001", "Berlin"]);
    }

    [TestMethod]
    public void UnionAllWithEmptyKeys_ShouldAppendRows()
    {
        var query = (
            "\n"
            + "            select Name, City from #A.Entities()\n"
            + "            union all ()\n"
            + "            select Name, City from #B.Entities()"
        );

        var table = CreateAndRunVirtualMachine(query, CreateSameNameDifferentCitySources())
            .Run(TestContext.CancellationToken);

        AssertNameCityRows(table, ["001", "Warsaw"], ["001", "Berlin"]);
    }

    [TestMethod]
    public void UnionAllWithExplicitKeys_ShouldAppendRowsAndPreserveDuplicates()
    {
        var query = (
            "\n"
            + "            select Name, City from #A.Entities()\n"
            + "            union all (Name)\n"
            + "            select Name, City from #B.Entities()"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001") { City = "Warsaw" }] },
            { "#B", [new BasicEntity("001") { City = "Warsaw" }] },
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        AssertNameCityRows(table, ["001", "Warsaw"], ["001", "Warsaw"]);
    }

    [TestMethod]
    public void ExceptWithOmittedKeys_ShouldCompareAllProjectedValues()
    {
        var query = (
            "\n"
            + "            select Name, City from #A.Entities()\n"
            + "            except\n"
            + "            select Name, City from #B.Entities()"
        );

        var table = CreateAndRunVirtualMachine(query, CreateSameNameDifferentCitySources())
            .Run(TestContext.CancellationToken);

        AssertNameCityRows(table, ["001", "Warsaw"]);
    }

    [TestMethod]
    public void ExceptWithEmptyKeys_ShouldCompareAllProjectedValues()
    {
        var query = (
            "\n"
            + "            select Name, City from #A.Entities()\n"
            + "            except ()\n"
            + "            select Name, City from #B.Entities()"
        );

        var table = CreateAndRunVirtualMachine(query, CreateSameNameDifferentCitySources())
            .Run(TestContext.CancellationToken);

        AssertNameCityRows(table, ["001", "Warsaw"]);
    }

    [TestMethod]
    public void IntersectWithOmittedKeys_ShouldCompareAllProjectedValues()
    {
        var query = (
            "\n"
            + "            select Name, City from #A.Entities()\n"
            + "            intersect\n"
            + "            select Name, City from #B.Entities()"
        );

        var table = CreateAndRunVirtualMachine(query, CreateSameNameDifferentCitySources())
            .Run(TestContext.CancellationToken);

        AssertNameCityRows(table);
    }

    [TestMethod]
    public void IntersectWithEmptyKeys_ShouldCompareAllProjectedValues()
    {
        var query = (
            "\n"
            + "            select Name, City from #A.Entities()\n"
            + "            intersect ()\n"
            + "            select Name, City from #B.Entities()"
        );

        var table = CreateAndRunVirtualMachine(query, CreateSameNameDifferentCitySources())
            .Run(TestContext.CancellationToken);

        AssertNameCityRows(table);
    }

    [TestMethod]
    public void ExplicitKeys_ShouldStillCompareOnlyRequestedSubset()
    {
        var query = (
            "\n"
            + "            select Name, City from #A.Entities()\n"
            + "            union (Name)\n"
            + "            select Name, City from #B.Entities()"
        );

        var table = CreateAndRunVirtualMachine(query, CreateSameNameDifferentCitySources())
            .Run(TestContext.CancellationToken);

        AssertNameCityRows(table, ["001", "Warsaw"]);
    }

    [TestMethod]
    public void ChainedSetOperators_ShouldMixOmittedEmptyAndExplicitKeys()
    {
        var query = (
            "\n"
            + "            select 1 as Id, 'A' as Tag from #A.Entities()\n"
            + "            union\n"
            + "            select 1 as Id, 'A' as Tag from #A.Entities()\n"
            + "            union ()\n"
            + "            select 1 as Id, 'B' as Tag from #A.Entities()\n"
            + "            union (Id)\n"
            + "            select 1 as Id, 'C' as Tag from #A.Entities()"
        );

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity("001")))
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Tag", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1, "A"]);
    }

    [TestMethod]
    public void UnionWithOmittedKeys_WhenProjectedTypesDiffer_ShouldStillFail()
    {
        var query = (
            "\n"
            + "            select Name from #A.Entities()\n"
            + "            union\n"
            + "            select 1 as Name from #A.Entities()"
        );

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity("001")))
        );

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3020_SetOperatorColumnTypes, DiagnosticPhase.Bind, "same types");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void UnionWithOmittedKeys_WhenNullColumnIsPartOfImplicitKey_ShouldInferConcreteType()
    {
        var query = (
            "\n"
            + "            select Name, null as Extra from #A.Entities()\n"
            + "            union\n"
            + "            select Name, City as Extra from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("001") { City = "Warsaw" }] },
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Extra", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { "001", null }, ["001", "Warsaw"]);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSameNameDifferentCitySources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001") { City = "Warsaw" }] },
            { "#B", [new BasicEntity("001") { City = "Berlin" }] },
        };
    }

    private static void AssertNameCityRows(Tables.Table table, params object?[][] rows)
    {
        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, rows);
    }
}
