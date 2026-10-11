using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.SourcePlanning;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class NegatedPredicateTests : BasicEntityTestBase
{
    private sealed record ExecutionScenario(string Shape, SourcePlanningMode Mode,
        IReadOnlyDictionary<string, IReadOnlyList<SourcePlanningEntity>> Rows);

    private static readonly SourcePlanningMode[] Modes =
    [SourcePlanningMode.RejectAll, SourcePlanningMode.AcceptPredicate, SourcePlanningMode.AcceptFirstPredicate];

    public static IEnumerable<object[]> GeneratedExtractionCases =>
        Enumerable.Range(0, 256).Select(index => new object[] { index });

    public static IEnumerable<object[]> GeneratedExecutionCases =>
        from block in Enumerable.Range(0, 16)
        from root in Enumerable.Range(0, 4)
        from shape in new[] { "direct", "cte" }
        from mode in Modes
        select new object[] { block * 16 + root, shape, mode };

    [TestMethod]
    [DynamicData(nameof(GeneratedExtractionCases))]
    public void GeneratedPredicate_WhenExtracted_ShouldNeverRejectMatchingPair(int index)
    {
        AssertExtractionSafety(GeneratedPredicates[index]);
    }

    [TestMethod]
    [DynamicData(nameof(GeneratedExecutionCases))]
    public void GeneratedPredicate_WhenExecuted_ShouldMatchIndependentOracle(int index, string shape, SourcePlanningMode mode)
    {
        var predicate = GeneratedPredicates[index];
        AssertPairs(predicate.Sql, predicate.Evaluate, new ExecutionScenario(shape, mode, CreateRows()));
    }

    private static void AssertExtractionSafety(TestPredicate predicate)
    {
        foreach (var alias in new[] { "l", "r" })
        {
            var source = new SchemaFromNode("#test", "items", new ArgsListNode([]), alias, typeof(object), 0);
            var visitor = new RewriteWhereExpressionToPassItToDataSourceVisitor(source);
            new WhereNode(predicate.ToNode()).Accept(new RewriteWhereExpressionToPassItToDataSourceTraverseVisitor(visitor));
            var extracted = visitor.WhereNode.Expression;
            AssertLocalReferences(extracted, alias, predicate.Sql);
            foreach (var left in new[] { -1, 0, 1, 2, 3, 4 })
            foreach (var right in new[] { -1, 0, 1, 2, 3, 4 })
            {
                if (!predicate.Evaluate(left, right))
                    continue;
                Assert.IsTrue(EvaluateExtracted(extracted, left, right),
                    $"Original: {predicate.Sql}; source: {alias}; extracted: {extracted.ToString()}; left={left}; right={right}");
            }
        }
    }

    private static void AssertLocalReferences(Node node, string alias, string original)
    {
        switch (node)
        {
            case AccessColumnNode column:
                Assert.AreEqual(alias, column.Alias, $"Original: {original}; extracted column: {column}");
                break;
            case BinaryNode binary:
                AssertLocalReferences(binary.Left, alias, original);
                AssertLocalReferences(binary.Right, alias, original);
                break;
            case UnaryNode unary:
                AssertLocalReferences(unary.Expression, alias, original);
                break;
        }
    }

    private void AssertPairs(string predicate, Func<int, int, bool> accepts, ExecutionScenario scenario)
    {
        var rows = scenario.Rows;
        var expected = (from left in rows["#left"]
                        from right in rows["#right"]
                        where left.JoinKey == right.JoinKey && accepts(left.Score, right.Score)
                        select $"{left.Id}:{right.Id}").Order(StringComparer.Ordinal).ToArray();
        var query = PairQuery(predicate, scenario.Shape);
        var provider = new SourcePlanningSchemaProvider(scenario.Mode, rows);
        var compiled = CreateAndRunVirtualMachine(query, schemaProvider: provider);
        var table = TableMaterializationTestHelper.Materialize(compiled.Run());
        var actual = table.Select(row => $"{row.ReadValue(0)}:{row.ReadValue(1)}").Order(StringComparer.Ordinal).ToArray();
        CollectionAssert.AreEqual(expected, actual,
            $"Predicate: {predicate}; shape: {scenario.Shape}; mode: {scenario.Mode}; expected: [{string.Join(",", expected)}]; actual: [{string.Join(",", actual)}]");
    }

    private static string PairQuery(string predicate, string shape)
    {
        var prefix = shape switch
        {
            "cte" => "with lefts as (select l.Id, l.Score, l.JoinKey, l.Name from #left.items() l), rights as (select r.Id, r.Score, r.JoinKey, r.Name from #right.items() r) ",
            "nested-cte" => "with left0 as (select l.Id, l.Score, l.JoinKey, l.Name from #left.items() l), lefts as (select x.Id, x.Score, x.JoinKey, x.Name from left0 x), rights as (select r.Id, r.Score, r.JoinKey, r.Name from #right.items() r) ",
            _ => string.Empty
        };
        var sources = shape is "cte" or "nested-cte" ? "lefts l join rights r" : "#left.items() l join #right.items() r";
        if (shape == "on")
            return $"select l.Id, r.Id from {sources} on l.JoinKey = r.JoinKey and ({predicate})";
        return $"{prefix}select l.Id, r.Id from {sources} on l.JoinKey = r.JoinKey where {predicate}";
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<SourcePlanningEntity>> CreateRows() =>
        new Dictionary<string, IReadOnlyList<SourcePlanningEntity>>
        {
            ["#left"] = [Row(1, -1, 1), Row(2, 0, 1), Row(3, 1, 1), Row(4, 2, 2), Row(4, 2, 2), Row(5, 3, 9)],
            ["#right"] = [Row(10, 0, 1), Row(11, 1, 1), Row(12, 3, 1), Row(13, 2, 2), Row(14, 4, 2), Row(15, -1, 8)]
        };

    private static SourcePlanningEntity Row(int id, int score, int key) =>
        new() { Id = id, Score = score, JoinKey = key, Name = $"item-{score}", Category = $"file-{key}" };
}
