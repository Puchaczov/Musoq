using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Bug probes for CTE/table/column alias behavior that differs from mainstream SQL engines.
///     The comments describe the PostgreSQL / SQL Server behavior these should eventually match.
/// </summary>
[TestClass]
public class AliasCteBugProbeTests : BasicEntityTestBase
{

    [TestMethod]
    public void CteQualifiedProjection_ShouldExposeUnqualifiedOutputColumnName()
    {
        // PostgreSQL and SQL Server expose this CTE column as "City".
        const string query = @"
            with p as (
                select a.City from #A.Entities() a
            )
            select City from p";

        var vm = CreateAndRunVirtualMachine(query, CreateSources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        CollectionAssert.AreEquivalent(
            new[] { "WARSAW", "BERLIN" },
            table.Select(row => (string)row.Values[0]).ToArray());
    }

    [TestMethod]
    public void CteQualifiedProjection_ShouldRejectLegacyRawBracketedLeakedName()
    {
        // The source qualifier "a" is scoped to the CTE body, so "[a.City]" is not a CTE output column.
        const string query = @"
            with p as (
                select a.City from #A.Entities() a
            )
            select [a.City] from p";

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateSources()));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "a.City");
    }

    [TestMethod]
    public void CteExplicitDottedAlias_ShouldRemainAccessibleByRawBracketedName()
    {
        const string query = @"
            with p as (
                select a.City as [a.City] from #A.Entities() a
            )
            select [a.City] from p";

        var vm = CreateAndRunVirtualMachine(query, CreateSources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        CollectionAssert.AreEquivalent(
            new[] { "WARSAW", "BERLIN" },
            table.Select(row => (string)row.Values[0]).ToArray());
    }

    [TestMethod]
    public void CteName_ShouldNotReserveOuterBaseAliasWhenCteHasDifferentAlias()
    {
        // Mainstream SQL allows the CTE relation "src" to be referenced as alias "c",
        // while an unrelated base source in the same FROM clause is also aliased "src".
        const string query = @"
            with src as (
                select City from #A.Entities()
            )
            select src.City, c.City
            from #A.Entities() src
            inner join src c on src.City = c.City";

        var vm = CreateAndRunVirtualMachine(query, CreateSources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        CollectionAssert.AreEquivalent(
            new[] { "WARSAW", "BERLIN" },
            table.Select(row => (string)row.Values[0]).ToArray());
        CollectionAssert.AreEquivalent(
            new[] { "WARSAW", "BERLIN" },
            table.Select(row => (string)row.Values[1]).ToArray());
    }

    [TestMethod]
    public void CteOutputColumnNamedLikeCte_ShouldResolveUnqualifiedIdentifierAsColumn()
    {
        // PostgreSQL and SQL Server resolve SELECT cte here as the CTE output column named "cte".
        // Musoq treats the identifier as a table/CTE reference and reaches an unsupported raw expression.
        const string query = @"
            with cte as (
                select City as cte from #A.Entities()
            )
            select cte from cte";

        var vm = CreateAndRunVirtualMachine(query, CreateSources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("cte", table.Columns.ElementAt(0).ColumnName);
        CollectionAssert.AreEquivalent(
            new[] { "WARSAW", "BERLIN" },
            table.Select(row => (string)row.Values[0]).ToArray());
    }

    [TestMethod]
    public void CteNameReusedInsideCteBody_ShouldNotPoisonCteRowShape()
    {
        // The alias inside the CTE body should be scoped to that inner query.
        // Musoq currently compiles a broken CTE/source shape and later asks the source for object rows.
        const string query = @"
            with src as (
                select City from #A.Entities() src
            )
            select src.City from src src";

        var vm = CreateAndRunVirtualMachine(query, CreateSources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        CollectionAssert.AreEquivalent(
            new[] { "WARSAW", "BERLIN" },
            table.Select(row => (string)row.Values[0]).ToArray());
    }

    [TestMethod]
    public void CteUsedWithAlias_ShouldBeReferencedThroughThatAlias()
    {
        const string query = @"
            with p as (
                select City from #A.Entities()
            )
            select c.City from p c";

        var vm = CreateAndRunVirtualMachine(query, CreateSources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        CollectionAssert.AreEquivalent(
            new[] { "WARSAW", "BERLIN" },
            table.Select(row => (string)row.Values[0]).ToArray());
    }

    [TestMethod]
    public void CteUsedWithAlias_ShouldHideOriginalCteName()
    {
        const string query = @"
            with p as (
                select City from #A.Entities()
            )
            select p.City from p c";

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateSources()));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind, "p");
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };
    }
}
