using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.RecursiveCte;

namespace Musoq.Evaluator.Tests;

public sealed partial class QueryResultOracleTests
{
    [TestMethod]
    [DataRow(7, "union", 12)]
    [DataRow(19, "union", 12)]
    [DataRow(43, "union", 12)]
    [DataRow(97, "union", 12)]
    [DataRow(997, "union", 12)]
    [DataRow(2026, "union", 12)]
    [DataRow(7, "union all", 12)]
    [DataRow(19, "union all", 12)]
    [DataRow(43, "union all", 12)]
    [DataRow(97, "union all", 12)]
    [DataRow(997, "union all", 12)]
    [DataRow(2026, "union all", 12)]
    [DataRow(7, "union", 0)]
    [DataRow(7, "union", 1)]
    [DataRow(7, "union all", 0)]
    [DataRow(7, "union all", 1)]
    public void RecursiveGraphs_ShouldMatchIndependentFrontierTraversal(int seed, string setOperator, int nodeBudget)
    {
        var input = OracleGraphInputs(seed, setOperator, nodeBudget);
        var expected = ExpectedGraphRows(input, setOperator);
        var sql = $"""
            with recursive walk (Id) as (
                select RootId from #graph.roots()
                {setOperator}
                select e.TargetId from walk w
                inner join #graph.edges() e on e.SourceId = w.Id
            )
            select Id from walk order by Id
            """;
        using var query = CreateAndRunVirtualMachine(sql, schemaProvider: new RecursiveGraphSchemaProvider(input));
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(result, expected);
    }

    private RecursiveGraphData OracleGraphInputs(int seed, string setOperator, int nodeBudget)
    {
        var edges = new List<RecursiveGraphEdge>();
        var roots = new List<RecursiveGraphRoot>();
        if (nodeBudget > 0)
            roots.Add(new RecursiveGraphRoot(1));
        if (nodeBudget > 1)
        {
            roots.Add(new RecursiveGraphRoot(1));
            edges.AddRange([
                new(1, 2, 1m, "duplicate-a"),
                new(1, 2, 1m, "duplicate-b"),
                new(1, 3, 1m, "branch"),
                new(2, 4, 1m, "diamond-left"),
                new(3, 4, 1m, "diamond-right"),
                new(4, 5, 1m, "chain"),
                new(5, 6, 1m, "last-level"),
                new(10, 11, 1m, "unreachable"),
            ]);
            var random = new Random(seed);
            if (random.Next(2) == 0)
                edges.Add(new RecursiveGraphEdge(2, 5, 1m, "shortcut"));
            if (random.Next(2) == 0)
                edges.Add(new RecursiveGraphEdge(3, 5, 1m, "other-shortcut"));
            if (setOperator == "union")
                edges.Add(new RecursiveGraphEdge(5, 1, 1m, "cycle"));
        }
        TestContext.WriteLine(
            "Graph seed={0}; operator={1}; roots={2}; edges={3}",
            seed,
            setOperator,
            string.Join(",", roots.Select(root => root.RootId)),
            string.Join(";", edges.Select(edge => $"{edge.SourceId}>{edge.TargetId}"))
        );
        return new RecursiveGraphData { Roots = roots, Edges = edges };
    }

    private static object?[][] ExpectedGraphRows(RecursiveGraphData input, string setOperator)
    {
        var frontier = input.Roots.Select(root => root.RootId).ToArray();
        var visited = new HashSet<int>();
        if (setOperator == "union")
        {
            frontier = frontier.Distinct().ToArray();
            visited.UnionWith(frontier);
        }
        var admitted = new List<int>();
        while (frontier.Length > 0)
        {
            admitted.AddRange(frontier);
            var next = frontier
                .SelectMany(id => input.Edges.Where(edge => edge.SourceId == id).Select(edge => edge.TargetId))
                .ToArray();
            if (setOperator == "union")
            {
                next = next.Where(id => !visited.Contains(id)).Distinct().ToArray();
                visited.UnionWith(next);
            }
            frontier = next;
        }
        return admitted.OrderBy(id => id).Select(id => new object?[] { id }).ToArray();
    }
}
