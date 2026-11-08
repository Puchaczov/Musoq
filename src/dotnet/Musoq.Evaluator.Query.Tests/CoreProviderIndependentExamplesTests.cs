using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class CoreProviderIndependentExamplesTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("42i", 42, typeof(int))]
    [DataRow("255ub", (byte)255, typeof(byte))]
    [DataRow("100ui", 100u, typeof(uint))]
    [DataRow("1l", 1L, typeof(long))]
    [DataRow("256 + 256 / 2", 384, typeof(int))]
    [DataRow("'Hello' || ' ' || 'World'", "Hello World", typeof(string))]
    public void SeedRow_ShouldEvaluateExpressionsWithoutARegisteredSource(string expression, object expected, Type type)
    {
        using var compiled = CreateAndRunVirtualMachine(
            $"select {expression} as Result from values {{ (Seed: 0) }} seed", EmptySources());
        var table = compiled.Run(TestContext.CancellationToken);
        _ = table.Count;

        TableMaterializationTestHelper.AssertColumns(table, ("Result", type));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [expected]);
    }

    [TestMethod]
    public void NullableInlineNames_ShouldProvideTheCoalesceColumnAndPreserveItsType()
    {
        using var compiled = CreateAndRunVirtualMachine(
            "select Name ?? 'Unknown' as DisplayName " +
            "from values { (Name: null), (Name: 'Ada') } people", EmptySources());
        var table = compiled.Run(TestContext.CancellationToken);
        _ = table.Count;

        TableMaterializationTestHelper.AssertColumns(table, ("DisplayName", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Unknown"], ["Ada"]);
    }

    [TestMethod]
    public void InlineCountryGroups_ShouldKeepNullGroupsAndIgnoreNullArguments()
    {
        using var compiled = CreateAndRunVirtualMachine(
            "select Country, Count(Country) as Present from " +
            "values { (Country: 'Poland'), (Country: null), (Country: null) } countries group by Country",
            EmptySources());
        var table = compiled.Run(TestContext.CancellationToken);
        _ = table.Count;

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Poland", 1L], [null, 0L]);
    }

    [TestMethod]
    public void InlineOuterJoin_ShouldRetainTheNullableDecimalContract()
    {
        using var compiled = CreateAndRunVirtualMachine(
            "select a.City, b.Population from values { (City: 'WARSAW'), (City: 'BERLIN') } a " +
            "left join values { (City: 'WARSAW', Population: 100d) } b on a.City = b.City",
            EmptySources());
        var table = compiled.Run(TestContext.CancellationToken);
        _ = table.Count;

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Population", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW", 100m], ["BERLIN", null]);
    }

    [TestMethod]
    public void InlineRanking_ShouldPreservePeersAndExplicitOutputOrder()
    {
        using var compiled = CreateAndRunVirtualMachine(
            "select Name, Rank() over (order by Population) as Position from " +
            "values { (Name: 'Ada', Population: 100), (Name: 'Bob', Population: 200), " +
            "(Name: 'Cara', Population: 200), (Name: 'Dan', Population: 400) } people order by Name",
            EmptySources());
        var table = compiled.Run(TestContext.CancellationToken);
        _ = table.Count;

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Ada", 1L], ["Bob", 2L], ["Cara", 2L], ["Dan", 4L]);
    }

    [TestMethod]
    [DataRow("select a.City from values { (City: 'WARSAW') } a where a.City in (select b.City, b.Country from values { (City: 'WARSAW', Country: 'Poland') } b)", DiagnosticCode.MQ3049_InSubqueryMultipleColumns)]
    [DataRow("select Name, City, Count(1) from values { (Name: 'Ada', City: 'WARSAW') } people group by City", DiagnosticCode.MQ3012_NonAggregateInSelect)]
    public void RejectedInlineExamples_ShouldKeepTheirIntendedDiagnostic(string query, DiagnosticCode expected)
    {
        var exception = Assert.ThrowsExactly<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, EmptySources()));

        Assert.AreEqual(expected, exception.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void InlinePagination_ShouldHaveEnoughRowsForTheStatedFiveRowPage()
    {
        const string query = "select Name from values { (Name: 'Person01'), (Name: 'Person02'), " +
            "(Name: 'Person03'), (Name: 'Person04'), (Name: 'Person05'), (Name: 'Person06'), " +
            "(Name: 'Person07'), (Name: 'Person08'), (Name: 'Person09'), (Name: 'Person10'), " +
            "(Name: 'Person11'), (Name: 'Person12'), (Name: 'Person13'), (Name: 'Person14'), " +
            "(Name: 'Person15') } people order by Name skip 10 take 5";
        var table = CreateAndRunVirtualMachine(query, EmptySources()).Run(TestContext.CancellationToken);
        _ = table.Count;

        TableMaterializationTestHelper.AssertRowsInOrder(table,
            ["Person11"], ["Person12"], ["Person13"], ["Person14"], ["Person15"]);
    }

    [TestMethod]
    public void InlineKeyedUnion_ShouldExposeConflictingPayloadAndAnActualOverlap()
    {
        const string query = "select Name, City from values { (Name: 'Ada', City: 'WARSAW'), " +
            "(Name: 'Bob', City: 'BERLIN') } firstRows union (Name) " +
            "select Name, City from values { (Name: 'Ada', City: 'WARSAW'), " +
            "(Name: 'Bob', City: 'ROME'), (Name: 'Eve', City: 'HAMBURG') } secondRows";
        var table = CreateAndRunVirtualMachine(query, EmptySources()).Run(TestContext.CancellationToken);
        _ = table.Count;

        TableMaterializationTestHelper.AssertRowsUnordered(table,
            ["Ada", "WARSAW"], ["Bob", "BERLIN"], ["Eve", "HAMBURG"]);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> EmptySources() => new();
}
