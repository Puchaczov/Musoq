using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.SourcePlanning;

namespace Musoq.Evaluator.Tests;

public sealed partial class NegatedPredicateTests
{
    [TestMethod]
    [DataRow("inner", "where", "NOT (r.NullableValue < l.NullableValue)", "2:10")]
    [DataRow("left", "where", "NOT (r.NullableValue < l.NullableValue)", "2:10")]
    [DataRow("right", "where", "NOT (r.NullableValue < l.NullableValue)", "2:10")]
    [DataRow("full outer", "where", "NOT (r.NullableValue < l.NullableValue)", "2:10")]
    [DataRow("left", "on", "NOT (r.NullableValue < l.NullableValue)", "1:null,2:10,3:null")]
    [DataRow("right", "on", "NOT (r.NullableValue < l.NullableValue)", "2:10,null:11,null:12")]
    [DataRow("full outer", "on", "NOT (r.NullableValue < l.NullableValue)", "1:null,2:10,3:null,null:11,null:12")]
    [DataRow("left", "where", "NOT (r.NullableValue is null)", "1:10,2:10")]
    [DataRow("left", "where", "NOT (r.NullableValue < l.NullableValue OR r.NullableValue is null)", "2:10")]
    [DataRow("left", "where", "NOT (r.NullableValue < l.NullableValue AND l.NullableValue is null)", "2:10,2:11,3:null")]
    [DataRow("left", "where", "NOT (r.NullableValue < l.NullableValue OR l.NullableValue is null)", "2:10")]
    [DataRow("left", "where", "NOT (r.NullableValue < l.NullableValue AND 1 = 0)", "1:10,1:11,2:10,2:11,3:null")]
    [DataRow("left", "where", "NOT (NOT (NOT (r.NullableValue < l.NullableValue OR l.NullableValue is null)))", "2:10")]
    [DataRow("right", "where", "NOT (l.NullableValue < r.NullableValue AND r.NullableValue is null)", "1:10,2:10,null:12")]
    public void WhenNullableNegationCrossesJoinBoundary_ShouldPreserveThreeValuedSemantics(
        string join, string placement, string predicate, string expected)
    {
        var tail = placement == "on" ? $"and ({predicate})" : $"where {predicate}";
        var compiled = CreateAndRunVirtualMachine(
            $"select l.Id, r.Id from #A.entities() l {join} join #B.entities() r on l.City = r.City {tail}",
            NullableRows());
        var actual = TableMaterializationTestHelper.Materialize(compiled.Run())
            .Select(row => $"{row.ReadValue(0) ?? "null"}:{row.ReadValue(1) ?? "null"}").Order(StringComparer.Ordinal).ToArray();

        CollectionAssert.AreEqual(expected.Split(',').Order(StringComparer.Ordinal).ToArray(), actual,
            $"Predicate: {predicate}; expected: {expected}; actual: {string.Join(",", actual)}");
    }

    [TestMethod]
    public void WhenSingleSourceNegationHasNullableOperand_ShouldExcludeUnknownRows()
    {
        var compiled = CreateAndRunVirtualMachine(
            "select l.Id from #A.entities() l where NOT (l.NullableValue < 2)", NullableRows());
        var actual = TableMaterializationTestHelper.Materialize(compiled.Run()).Select(row => (int)row.ReadValue(0)!).ToArray();

        CollectionAssert.AreEqual(new[] { 3 }, actual);
    }

    [TestMethod]
    public void WhenCrossSourceNegationIsResidual_ShouldNotOfferItToEitherProvider()
    {
        var provider = new SourcePlanningSchemaProvider(SourcePlanningMode.AcceptPredicate, CreateRows());
        var compiled = CreateAndRunVirtualMachine(PairQuery("NOT (r.Score < l.Score)", "direct"), schemaProvider: provider);
        compiled.Run();

        Assert.IsTrue(provider.Requests.All(request => request.Predicate == null),
            "A cross-source negated condition cannot be a provider-local predicate.");
    }

    [TestMethod]
    [DataRow("r.NullableValue < 0 AND l.NullableValue < 0", "1:null,2:null,3:False")]
    [DataRow("r.NullableValue < 0 OR l.NullableValue < 0", "1:null,2:True,3:null")]
    [DataRow("NOT (r.NullableValue < 0 AND l.NullableValue < 0)", "1:null,2:null,3:True")]
    [DataRow("NOT (r.NullableValue < 0 OR l.NullableValue < 0)", "1:null,2:False,3:null")]
    [DataRow("NOT (NOT (r.NullableValue < 0 AND l.NullableValue < 0))", "1:null,2:null,3:False")]
    [DataRow("NOT (NOT (r.NullableValue < 0 OR l.NullableValue < 0))", "1:null,2:True,3:null")]
    [DataRow("NOT ((r.NullableValue < 0 OR l.NullableValue < 0) AND 1 = 0)", "1:True,2:True,3:True")]
    [DataRow("NOT ((r.NullableValue < 0 AND l.NullableValue < 0) OR 1 = 1)", "1:False,2:False,3:False")]
    public void WhenUnmatchedProjectionCombinesUnknownWithDynamicBoolean_ShouldPreserveTruthTable(
        string expression, string expected)
    {
        var rows = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new() { Id = 1, NullableValue = null }, new() { Id = 2, NullableValue = -1 }, new() { Id = 3, NullableValue = 1 }],
            ["#B"] = [new() { Id = 10, NullableValue = 0 }]
        };
        var compiled = CreateAndRunVirtualMachine(
            $"select l.Id, {expression} as Truth from #A.entities() l left join #B.entities() r on l.Id = r.Id", rows);
        var actual = TableMaterializationTestHelper.Materialize(compiled.Run())
            .Select(row => $"{row.ReadValue(0)}:{row.ReadValue(1) ?? "null"}").Order(StringComparer.Ordinal).ToArray();

        CollectionAssert.AreEqual(expected.Split(','), actual);
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> NullableRows() =>
        new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new() { Id = 1, City = "a", NullableValue = null }, new() { Id = 2, City = "a", NullableValue = 1 }, new() { Id = 3, City = "b", NullableValue = 2 }],
            ["#B"] = [new() { Id = 10, City = "a", NullableValue = 2 }, new() { Id = 11, City = "a", NullableValue = null }, new() { Id = 12, City = "c", NullableValue = 0 }]
        };
}
