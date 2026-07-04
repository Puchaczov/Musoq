using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenDerivedTableInFrom_ShouldMaterializeAndProject()
    {
        const string query = @"
            SELECT d.City FROM (
                SELECT a.City FROM #A.entities() a
                WHERE a.Population > 250
            ) d
            ORDER BY d.City";

        var table = CreateAndRunVirtualMachine(query, CreateDerivedSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("d.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["PARIS"], ["WARSAW"]);
    }

    [TestMethod]
    public void WhenDerivedTableProjectsAliases_ShouldExposeOnlyProjectedShapeToOuterQuery()
    {
        const string query = @"
            SELECT d.Location, d.Score FROM (
                SELECT a.City as Location, a.Population + 1 as Score
                FROM #A.entities() a
                WHERE a.Population >= 250
            ) d
            WHERE d.Score > 251
            ORDER BY d.Location";

        var table = CreateAndRunVirtualMachine(query, CreateDerivedSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("d.Location", typeof(string)),
            ("d.Score", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["PARIS", 301m], ["WARSAW", 501m]);
    }

    [TestMethod]
    public void WhenDerivedTableInJoin_ShouldJoinByAlias()
    {
        const string query = @"
            SELECT a.City, d.City FROM #A.entities() a
            INNER JOIN (
                SELECT b.Country, b.City FROM #B.entities() b
            ) d ON a.Country = d.Country
            ORDER BY a.City, d.City";

        var table = CreateAndRunVirtualMachine(query, CreateDerivedSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("d.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["PARIS", "LYON"],
            ["WARSAW", "GDANSK"],
            ["WARSAW", "KRAKOW"]);
    }

    [TestMethod]
    public void WhenDerivedTableUsesCte_ShouldResolveLocalCte()
    {
        const string query = @"
            SELECT d.City FROM (
                WITH src AS (
                    SELECT a.City FROM #A.entities() a
                    WHERE a.Population > 250
                )
                SELECT s.City FROM src s
            ) d
            ORDER BY d.City";

        var table = CreateAndRunVirtualMachine(query, CreateDerivedSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("d.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["PARIS"], ["WARSAW"]);
    }

    [TestMethod]
    public void WhenPlainDerivedTableReferencesOuterAlias_ShouldRejectImplicitLateral()
    {
        const string query = @"
            SELECT a.City, d.City FROM #A.entities() a
            INNER JOIN (
                SELECT b.City, b.Country FROM #B.entities() b
                WHERE b.Country = a.Country
            ) d ON a.Country = d.Country";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateDerivedSources()));

        Assert.IsTrue(exception.Envelopes.Any(envelope => envelope.Code == DiagnosticCode.MQ2024_InvalidSubquery));
        StringAssert.Contains(exception.Message, "Plain derived tables are not lateral");
        StringAssert.Contains(exception.Message, "Use CROSS APPLY or OUTER APPLY");
        StringAssert.Contains(exception.Message, "outer alias 'a'");
    }

    [TestMethod]
    public void WhenCrossApplyDerivedTableHasCorrelation_ShouldDecorrelateToInnerJoin()
    {
        const string query = @"
            SELECT a.City, d.City FROM #A.entities() a
            CROSS APPLY (
                SELECT b.City, b.Country FROM #B.entities() b
                WHERE b.Country = a.Country
            ) d
            ORDER BY a.City, d.City";

        var table = CreateAndRunVirtualMachine(query, CreateDerivedSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("d.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["PARIS", "LYON"],
            ["WARSAW", "GDANSK"],
            ["WARSAW", "KRAKOW"]);
    }

    [TestMethod]
    public void WhenOuterApplyDerivedTableHasNoMatches_ShouldPreserveLeftRows()
    {
        const string query = @"
            SELECT a.City, d.City FROM #A.entities() a
            OUTER APPLY (
                SELECT b.City, b.Country FROM #B.entities() b
                WHERE b.Country = a.Country
            ) d
            ORDER BY a.City, d.City";

        var table = CreateAndRunVirtualMachine(query, CreateDerivedSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("d.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            new object?[] { "BERLIN", null },
            ["PARIS", "LYON"],
            ["WARSAW", "GDANSK"],
            ["WARSAW", "KRAKOW"]);
    }

    [TestMethod]
    public void WhenCrossApplyDerivedTableUsesSetOperatorWithCorrelation_ShouldDecorrelateBranches()
    {
        const string query = @"
            SELECT a.City, d.City FROM #A.entities() a
            CROSS APPLY (
                SELECT b.City, b.Country FROM #B.entities() b
                WHERE b.Country = a.Country
                UNION (City, Country)
                SELECT c.City, c.Country FROM #C.entities() c
                WHERE c.Country = a.Country
            ) d
            ORDER BY a.City, d.City";

        var table = CreateAndRunVirtualMachine(query, CreateDerivedSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("d.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["BERLIN", "MUNICH"],
            ["PARIS", "LYON"],
            ["PARIS", "NICE"],
            ["WARSAW", "GDANSK"],
            ["WARSAW", "KRAKOW"],
            ["WARSAW", "POZNAN"]);
    }

    [TestMethod]
    public void WhenCrossApplyDerivedTableSetBranchDoesNotProjectCorrelationKey_ShouldReject()
    {
        const string query = @"
            SELECT a.City, d.City FROM #A.entities() a
            CROSS APPLY (
                SELECT b.City, b.Country FROM #B.entities() b
                WHERE b.Country = a.Country
                UNION (City)
                SELECT c.City FROM #C.entities() c
                WHERE c.Country = a.Country
            ) d";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateDerivedSources()));

        Assert.IsTrue(exception.Envelopes.Any(envelope => envelope.Code == DiagnosticCode.MQ2024_InvalidSubquery));
        StringAssert.Contains(exception.Message, "Correlated APPLY derived table");
        StringAssert.Contains(exception.Message, "project");
        StringAssert.Contains(exception.Message, "correlation column");
    }

    [TestMethod]
    public void WhenDerivedTableAliasShadowsOuterAlias_ShouldNotTreatAsCorrelation()
    {
        const string query = @"
            SELECT a.City, d.City FROM #A.entities() a
            INNER JOIN (
                SELECT a.City, a.Country FROM #B.entities() a
            ) d ON a.Country = d.Country
            ORDER BY a.City, d.City";

        var table = CreateAndRunVirtualMachine(query, CreateDerivedSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("d.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["PARIS", "LYON"],
            ["WARSAW", "GDANSK"],
            ["WARSAW", "KRAKOW"]);
    }

    [TestMethod]
    public void WhenCorrelatedApplyDoesNotProjectLocalKey_ShouldRejectHiddenCorrelationColumn()
    {
        const string query = @"
            SELECT a.City, d.City FROM #A.entities() a
            CROSS APPLY (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
            ) d";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateDerivedSources()));

        Assert.IsTrue(exception.Envelopes.Any(envelope => envelope.Code == DiagnosticCode.MQ2024_InvalidSubquery));
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateDerivedSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            },
            {
                "#B", [
                    new BasicEntity("KRAKOW", "POLAND", 100),
                    new BasicEntity("GDANSK", "POLAND", 400),
                    new BasicEntity("LYON", "FRANCE", 200)
                ]
            },
            {
                "#C", [
                    new BasicEntity("POZNAN", "POLAND", 120),
                    new BasicEntity("NICE", "FRANCE", 130),
                    new BasicEntity("MUNICH", "GERMANY", 140)
                ]
            }
        };
    }
}
