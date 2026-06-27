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
    public void UnionWithOmittedKeys_ShouldCompareAllProjectedValues()
    {
        var query = @"
            select Name, City from #A.Entities()
            union
            select Name, City from #B.Entities()";

        var table = CreateAndRunVirtualMachine(query, CreateSameNameDifferentCitySources())
            .Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        AssertContainsRow(table, "001", "Warsaw");
        AssertContainsRow(table, "001", "Berlin");
    }

    [TestMethod]
    public void UnionWithEmptyKeys_ShouldCompareAllProjectedValues()
    {
        var query = @"
            select Name, City from #A.Entities()
            union ()
            select Name, City from #B.Entities()";

        var table = CreateAndRunVirtualMachine(query, CreateSameNameDifferentCitySources())
            .Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        AssertContainsRow(table, "001", "Warsaw");
        AssertContainsRow(table, "001", "Berlin");
    }

    [TestMethod]
    public void UnionAllWithOmittedKeys_ShouldAppendRows()
    {
        var query = @"
            select Name, City from #A.Entities()
            union all
            select Name, City from #B.Entities()";

        var table = CreateAndRunVirtualMachine(query, CreateSameNameDifferentCitySources())
            .Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        AssertContainsRow(table, "001", "Warsaw");
        AssertContainsRow(table, "001", "Berlin");
    }

    [TestMethod]
    public void UnionAllWithEmptyKeys_ShouldAppendRows()
    {
        var query = @"
            select Name, City from #A.Entities()
            union all ()
            select Name, City from #B.Entities()";

        var table = CreateAndRunVirtualMachine(query, CreateSameNameDifferentCitySources())
            .Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        AssertContainsRow(table, "001", "Warsaw");
        AssertContainsRow(table, "001", "Berlin");
    }

    [TestMethod]
    public void ExceptWithOmittedKeys_ShouldCompareAllProjectedValues()
    {
        var query = @"
            select Name, City from #A.Entities()
            except
            select Name, City from #B.Entities()";

        var table = CreateAndRunVirtualMachine(query, CreateSameNameDifferentCitySources())
            .Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        AssertContainsRow(table, "001", "Warsaw");
    }

    [TestMethod]
    public void ExceptWithEmptyKeys_ShouldCompareAllProjectedValues()
    {
        var query = @"
            select Name, City from #A.Entities()
            except ()
            select Name, City from #B.Entities()";

        var table = CreateAndRunVirtualMachine(query, CreateSameNameDifferentCitySources())
            .Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        AssertContainsRow(table, "001", "Warsaw");
    }

    [TestMethod]
    public void IntersectWithOmittedKeys_ShouldCompareAllProjectedValues()
    {
        var query = @"
            select Name, City from #A.Entities()
            intersect
            select Name, City from #B.Entities()";

        var table = CreateAndRunVirtualMachine(query, CreateSameNameDifferentCitySources())
            .Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void IntersectWithEmptyKeys_ShouldCompareAllProjectedValues()
    {
        var query = @"
            select Name, City from #A.Entities()
            intersect ()
            select Name, City from #B.Entities()";

        var table = CreateAndRunVirtualMachine(query, CreateSameNameDifferentCitySources())
            .Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void ExplicitKeys_ShouldStillCompareOnlyRequestedSubset()
    {
        var query = @"
            select Name, City from #A.Entities()
            union (Name)
            select Name, City from #B.Entities()";

        var table = CreateAndRunVirtualMachine(query, CreateSameNameDifferentCitySources())
            .Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        AssertContainsRow(table, "001", "Warsaw");
    }

    [TestMethod]
    public void ChainedSetOperators_ShouldMixOmittedEmptyAndExplicitKeys()
    {
        var query = @"
            select 1 as Id, 'A' as Tag from #A.Entities()
            union
            select 1 as Id, 'A' as Tag from #A.Entities()
            union ()
            select 1 as Id, 'B' as Tag from #A.Entities()
            union (Id)
            select 1 as Id, 'C' as Tag from #A.Entities()";

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity("001")))
            .Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => Convert.ToInt32(row.Values[0]) == 1 && (string)row.Values[1] == "A"));
        Assert.IsTrue(table.Any(row => Convert.ToInt32(row.Values[0]) == 1 && (string)row.Values[1] == "B"));
        Assert.IsFalse(table.Any(row => (string)row.Values[1] == "C"));
    }

    [TestMethod]
    public void UnionWithOmittedKeys_WhenProjectedTypesDiffer_ShouldStillFail()
    {
        var query = @"
            select Name from #A.Entities()
            union
            select 1 as Name from #A.Entities()";

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity("001"))));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3020_SetOperatorColumnTypes, DiagnosticPhase.Bind, "same types");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void UnionWithOmittedKeys_WhenNullColumnIsPartOfImplicitKey_ShouldInferConcreteType()
    {
        var query = @"
            select Name, null as Extra from #A.Entities()
            union
            select Name, City as Extra from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("001") { City = "Warsaw" }] }
        };

        var table = CreateAndRunVirtualMachine(query, sources)
            .Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "001" && row.Values[1] == null));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "001" && (string)row.Values[1] == "Warsaw"));
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSameNameDifferentCitySources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001") { City = "Warsaw" }] },
            { "#B", [new BasicEntity("001") { City = "Berlin" }] }
        };
    }

    private static void AssertContainsRow(Musoq.Evaluator.Tables.Table table, string name, string city)
    {
        Assert.IsTrue(
            table.Any(row => (string)row.Values[0] == name && (string)row.Values[1] == city),
            $"Expected row ({name}, {city}).");
    }
}
